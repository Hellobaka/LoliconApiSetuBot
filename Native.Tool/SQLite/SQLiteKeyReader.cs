/********************************************************
 * ADO.NET 2.0 Data Provider for SQLite Version 3.X
 * Written by Robert Simpson (robert@blackcastlesoft.com)
 *
 * Released to the public domain, use at your own risk!
 ********************************************************/

namespace System.Data.SQLite
{
  using System;
  using System.Data;
  using System.Data.Common;
  using System.Collections.Generic;
  using System.Globalization;

  /// <summary>
  /// This class provides key info for a given SQLite statement.
  /// <remarks>
  /// Providing key information for a given statement is non-trivial :(
  /// </remarks>
  /// </summary>
  internal sealed class SQLiteKeyReader : IDisposable
  {
    private KeyInfo[] _keyInfo;
    private SQLiteStatement _stmt;
    private bool _isValid;
    private RowIdInfo[] _rowIdInfo;

    /// <summary>
    /// Used to support CommandBehavior.KeyInfo
    /// </summary>
    private struct KeyInfo
    {
      internal string databaseName;
      internal string tableName;
      internal string columnName;
      internal int database;
      internal int rootPage;
      internal int cursor;
      internal KeyQuery query;
      internal int column;
    }

    /// <summary>
    /// Used to keep track of the per-table RowId column metadata.
    /// </summary>
    private struct RowIdInfo
    {
        internal string databaseName;
        internal string tableName;
        internal int column;
    }

    /// <summary>
    /// A single sub-query for a given table/database.
    /// </summary>
    private sealed class KeyQuery : IDisposable
    {
        private SQLiteCommand _command;
        internal SQLiteDataReader _reader;

        internal KeyQuery(SQLiteConnection cnn, string database, string table, params string[] columns)
        {
            using (SQLiteCommandBuilder builder = new SQLiteCommandBuilder())
            {
                _command = cnn.CreateCommand();
                for (int n = 0; n < columns.Length; n++)
                {
                    columns[n] = builder.QuoteIdentifier(columns[n]);
                }
            }
            _command.CommandText = HelperMethods.StringFormat(CultureInfo.InvariantCulture, "SELECT {0} FROM [{1}].[{2}] WHERE ROWID = ?", String.Join(",", columns), database, table);
            _command.Parameters.AddWithValue(null, (long)0);
        }

        internal bool IsValid
        {
            set
            {
                if (value != false) throw new ArgumentException();
                if (_reader != null)
                {
                    _reader.Dispose();
                    _reader = null;
                }
            }
        }

        internal void Sync(long rowid)
        {
            IsValid = false;
            _command.Parameters[0].Value = rowid;
            _reader = _command.ExecuteReader();
            _reader.Read();
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IDisposable Members
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region IDisposable "Pattern" Members
        private bool disposed;
        private void CheckDisposed() /* throw */
        {
#if THROW_ON_DISPOSED
            if (disposed)
                throw new ObjectDisposedException(typeof(KeyQuery).Name);
#endif
        }

        ///////////////////////////////////////////////////////////////////////////////////////////////

        private void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    ////////////////////////////////////
                    // dispose managed resources here...
                    ////////////////////////////////////

                    IsValid = false;

                    if (_command != null) _command.Dispose();
                    _command = null;
                }

                //////////////////////////////////////
                // release unmanaged resources here...
                //////////////////////////////////////

                disposed = true;
            }
        }
        #endregion

        ///////////////////////////////////////////////////////////////////////////////////////////////

        #region Destructor
        ~KeyQuery()
        {
            Dispose(false);
        }
        #endregion
    }

    /// <summary>
    /// This function does all the nasty work at determining what keys need to be returned for
    /// a given statement.
    /// </summary>
    /// <param name="cnn"></param>
    /// <param name="reader"></param>
    /// <param name="stmt"></param>
    internal SQLiteKeyReader(SQLiteConnection cnn, SQLiteDataReader reader, SQLiteStatement stmt)
    {
      Dictionary<string, int> catalogs = new Dictionary<string, int>();
      Dictionary<string, List<string>> tables = new Dictionary<string, List<string>>();
      List<string> list;
      List<KeyInfo> keys = new List<KeyInfo>();
      List<RowIdInfo> rowIds = new List<RowIdInfo>();

      // Record the statement so we can use it later for sync'ing
      _stmt = stmt;

      // Fetch all the attached databases on this connection
      using (DataTable tbl = cnn.GetSchema("Catalogs"))
      {
        foreach (DataRow row in tbl.Rows)
        {
          catalogs.Add((string)row["CATALOG_NAME"], Convert.ToInt32(row["ID"], CultureInfo.InvariantCulture));
        }
      }

      // Fetch all the unique tables and catalogs used by the current statement
      using (DataTable schema = reader.GetSchemaTable(false, false))
      {
        foreach (DataRow row in schema.Rows)
        {
          // Check if column is backed to a table
          if (row[SchemaTableOptionalColumn.BaseCatalogName] == DBNull.Value)
            continue;

          // Record the unique table so we can look up its keys
          string catalog = (string)row[SchemaTableOptionalColumn.BaseCatalogName];
          string table = (string)row[SchemaTableColumn.BaseTableName];

          if (tables.ContainsKey(catalog) == false)
          {
            list = new List<string>();
            tables.Add(catalog, list);
          }
          else
            list = tables[catalog];

          if (list.Contains(table) == false)
            list.Add(table);
        }

        // For each catalog and each table, query the indexes for the table.
        // Find a primary key index if there is one.  If not, find a unique index instead
        foreach (KeyValuePair<string, List<string>> pair in tables)
        {
          for (int i = 0; i < pair.Value.Count; i++)
          {
            string table = pair.Value[i];
            DataRow preferredRow = null;
            using (DataTable tbl = cnn.GetSchema("Indexes", new string[] { pair.Key, null, table }))
            {
              // Loop twice.  The first time looking for a primary key index,
              // the second time looking for a unique index
              for (int n = 0; n < 2 && preferredRow == null; n++)
              {
                foreach (DataRow row in tbl.Rows)
                {
                  if (n == 0 && (bool)row["PRIMARY_KEY"] == true)
                  {
                    preferredRow = row;
                    break;
                  }
                  else if (n == 1 && (bool)row["UNIQUE"] == true)
                  {
                    preferredRow = row;
                    break;
                  }
                }
              }
              if (preferredRow == null) // Unable to find any suitable index for this table so remove it
              {
                pair.Value.RemoveAt(i);
                i--;
              }
              else // We found a usable index, so fetch the necessary table details
              {
                using (DataTable tblTables = cnn.GetSchema("Tables", new string[] { pair.Key, null, table }))
                {
                  // Find the root page of the table in the current statement and get the cursor that's iterating it
                  int database = catalogs[pair.Key];
                  int rootPage = Convert.ToInt32(tblTables.Rows[0]["TABLE_ROOTPAGE"], CultureInfo.InvariantCulture);
                  int cursor = stmt._sql.GetCursorForTable(stmt, database, rootPage);

                  // Now enumerate the members of the index we're going to use
                  using (DataTable indexColumns = cnn.GetSchema("IndexColumns", new string[] { pair.Key, null, table, (string)preferredRow["INDEX_NAME"] }))
                  {
                    //
                    // NOTE: If this is actually a RowId (or alias), record that now.  There should
                    //       be exactly one index column in that case.
                    //
                    bool isRowId = (string)preferredRow["INDEX_NAME"] == "sqlite_master_PK_" + table;
                    KeyQuery query = null;

                    List<string> cols = new List<string>();
                    for (int x = 0; x < indexColumns.Rows.Count; x++)
                    {
                      string columnName = SQLiteConvert.GetStringOrNull(
                          indexColumns.Rows[x]["COLUMN_NAME"]);

                      bool addKey = true;
                      // If the column in the index already appears in the query, skip it
                      foreach (DataRow row in schema.Rows)
                      {
                        if (row.IsNull(SchemaTableColumn.BaseColumnName))
                          continue;

                        if ((string)row[SchemaTableColumn.BaseColumnName] == columnName &&
                            (string)row[SchemaTableColumn.BaseTableName] == table &&
                            (string)row[SchemaTableOptionalColumn.BaseCatalogName] == pair.Key)
                        {
                          if (isRowId)
                          {
                            RowIdInfo rowId = new RowIdInfo();

                            rowId.databaseName = pair.Key;
                            rowId.tableName = table;
                            rowId.column = (int)row[SchemaTableColumn.ColumnOrdinal];

                            rowIds.Add(rowId);
                          }
                          indexColumns.Rows.RemoveAt(x);
                          x--;
                          addKey = false;
                          break;
                        }
                      }
                      if (addKey == true)
                        cols.Add(columnName);
                    }

                    // If the index is not a rowid alias, record all the columns
                    // needed to make up the unique index and construct a SQL query for it
                    if (!isRowId)
                    {
                      // Whatever remains of the columns we need that make up the index that are not
                      // already in the query need to be queried separately, so construct a subquery
                      if (cols.Count > 0)
                      {
                        string[] querycols = new string[cols.Count];
                        cols.CopyTo(querycols);
                        query = new KeyQuery(cnn, pair.Key, table, querycols);
                      }
                    }

                    // Create a KeyInfo struct for each column of the index
                    for (int x = 0; x < indexColumns.Rows.Count; x++)
                    {
                      string columnName = SQLiteConvert.GetStringOrNull(indexColumns.Rows[x]["COLUMN_NAME"]);
                      KeyInfo key = new KeyInfo();

                      key.rootPage = rootPage;
                      key.cursor = cursor;
                      key.database = database;
                      key.databaseName = pair.Key;
                      key.tableName = table;
                      key.columnName = columnName;
                      key.query = query;
                      key.column = x;

                      keys.Add(key);
                    }
                  }
                }
              }
            }
          }
        }
      }

      // Now we have all the additional columns we have to return in order to support
      // CommandBehavior.KeyInfo
      _keyInfo = new KeyInfo[keys.Count];
      keys.CopyTo(_keyInfo);

      _rowIdInfo = new RowIdInfo[rowIds.Count];
      rowIds.CopyTo(_rowIdInfo);
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    internal int GetRowIdIndex(
        string databaseName,
        string tableName
        )
    {
        if ((_rowIdInfo != null) &&
            (databaseName != null) &&
            (tableName != null))
        {
            for (int i = 0; i < _rowIdInfo.Length; i++)
            {
                if (_rowIdInfo[i].databaseName == databaseName &&
                    _rowIdInfo[i].tableName == tableName)
                {
                    return _rowIdInfo[i].column;
                }
            }
        }

        return -1;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    internal long? GetRowId(
        string databaseName,
        string tableName
        )
    {
        if ((_keyInfo != null) &&
            (databaseName != null) &&
            (tableName != null))
        {
            for (int i = 0; i < _keyInfo.Length; i++)
            {
                if (_keyInfo[i].databaseName == databaseName &&
                    _keyInfo[i].tableName == tableName)
                {
                    long rowid = _stmt._sql.GetRowIdForCursor(_stmt, _keyInfo[i].cursor);

                    if (rowid != 0)
                        return rowid;
                }
            }
        }

        return null;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    #region IDisposable Members
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////////////////////////

    #region IDisposable "Pattern" Members
    private bool disposed;
    private void CheckDisposed() /* throw */
    {
#if THROW_ON_DISPOSED
        if (disposed)
            throw new ObjectDisposedException(typeof(SQLiteKeyReader).Name);
#endif
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    private void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                ////////////////////////////////////
                // dispose managed resources here...
                ////////////////////////////////////

                _stmt = null;

                if (_keyInfo != null)
                {
                    for (int n = 0; n < _keyInfo.Length; n++)
                    {
                        if (_keyInfo[n].query != null)
                            _keyInfo[n].query.Dispose();
                    }

                    _keyInfo = null;
                }
            }

            //////////////////////////////////////
            // release unmanaged resources here...
            //////////////////////////////////////

            disposed = true;
        }
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////////////////////////

    #region Destructor
    ~SQLiteKeyReader()
    {
        Dispose(false);
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// How many additional columns of keyinfo we're holding
    /// </summary>
    internal int Count
    {
      get { return (_keyInfo == null) ? 0 : _keyInfo.Length; }
    }

    private void Sync(int i)
    {
      Sync();
      if (_keyInfo[i].cursor == -1)
        throw new InvalidCastException();
    }

    /// <summary>
    /// Make sure all the subqueries are open and ready and sync'd with the current rowid
    /// of the table they're supporting
    /// </summary>
    private void Sync()
    {
      if (_isValid == true) return;

      KeyQuery last = null;

      for (int n = 0; n < _keyInfo.Length; n++)
      {
        if (_keyInfo[n].query == null || _keyInfo[n].query != last)
        {
          last = _keyInfo[n].query;

          if (last != null)
          {
            last.Sync(_stmt._sql.GetRowIdForCursor(_stmt, _keyInfo[n].cursor));
          }
        }
      }
      _isValid = true;
    }

    /// <summary>
    /// Release any readers on any subqueries
    /// </summary>
    internal void Reset()
    {
      _isValid = false;
      if (_keyInfo == null) return;

      for (int n = 0; n < _keyInfo.Length; n++)
      {
        if (_keyInfo[n].query != null)
          _keyInfo[n].query.IsValid = false;
      }
    }

    internal string GetDataTypeName(int i)
    {
      Sync();
      if (_keyInfo[i].query != null) return _keyInfo[i].query._reader.GetDataTypeName(_keyInfo[i].column);
      else return "integer";
    }

    internal TypeAffinity GetFieldAffinity(int i)
    {
      Sync();
      if (_keyInfo[i].query != null) return _keyInfo[i].query._reader.GetFieldAffinity(_keyInfo[i].column);
      else return TypeAffinity.Uninitialized;
    }

    internal Type GetFieldType(int i)
    {
      Sync();
      if (_keyInfo[i].query != null) return _keyInfo[i].query._reader.GetFieldType(_keyInfo[i].column);
      else return typeof(Int64);
    }

    internal string GetDatabaseName(int i)
    {
        return _keyInfo[i].databaseName;
    }

    internal string GetTableName(int i)
    {
        return _keyInfo[i].tableName;
    }

    internal string GetName(int i)
    {
      return _keyInfo[i].columnName;
    }

    internal int GetOrdinal(string name)
    {
      for (int n = 0; n < _keyInfo.Length; n++)
      {
        if (String.Compare(name, _keyInfo[n].columnName, StringComparison.OrdinalIgnoreCase) == 0) return n;
      }
      return -1;
    }

    internal SQLiteBlob GetBlob(int i, bool readOnly)
    {
      Sync(i);
      if (_keyInfo[i].query != null) return _keyInfo[i].query._reader.GetBlob(_keyInfo[i].column, readOnly);
      else throw new InvalidCastException();
    }

    internal bool GetBoolean(int i)
    {
      Sync(i);
      if (_keyInfo[i].query != null) return _keyInfo[i].query._reader.GetBoolean(_keyInfo[i].column);
      else throw new InvalidCastException();
    }

    internal byte GetByte(int i)
    {
      Sync(i);
      if (_keyInfo[i].query != null) return _keyInfo[i].query._reader.GetByte(_keyInfo[i].column);
      else throw new InvalidCastException();
    }

    internal long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferoffset, int length)
    {
      Sync(i);
      if (_keyInfo[i].query != null) return _keyInfo[i].query._reader.GetBytes(_keyInfo[i].column, fieldOffset, buffer, bufferoffset, length);
      else throw new InvalidCastException();
    }

    internal char GetChar(int i)
    {
      Sync(i);
      if (_keyInfo[i].query != null) return _keyInfo[i].query._reader.GetChar(_keyInfo[i].column);
      else throw new InvalidCastException();
    }

    internal long GetChars(int i, long fieldOffset, char[] buffer, int bufferoffset, int length)
    {
      Sync(i);
      if (_keyInfo[i].query != null) return _keyInfo[i].query._reader.GetChars(_keyInfo[i].column, fieldOffset, buffer, bufferoffset, length);
      else throw new InvalidCastException();
    }

    internal DateTime GetDateTime(int i)
    {
      Sync(i);
      if (_keyInfo[i].query != null) return _keyInfo[i].query._reader.GetDateTime(_keyInfo[i].column);
      else throw new InvalidCastException();
    }

    internal decimal GetDecimal(int i)
    {
      Sync(i);
      if (_keyInfo[i].query != null) return _keyInfo[i].query._reader.GetDecimal(_keyInfo[i].column);
      else throw new InvalidCastException();
    }

    internal double GetDouble(int i)
    {
      Sync(i);
      if (_keyInfo[i].query != null) return _keyInfo[i].query._reader.GetDouble(_keyInfo[i].column);
      else throw new InvalidCastException();
    }

    internal float GetFloat(int i)
    {
      Sync(i);
      if (_keyInfo[i].query != null) return _keyInfo[i].query._reader.GetFloat(_keyInfo[i].column);
      else throw new InvalidCastException();
    }

    internal Guid GetGuid(int i)
    {
      Sync(i);
      if (_keyInfo[i].query != null) return _keyInfo[i].query._reader.GetGuid(_keyInfo[i].column);
      else throw new InvalidCastException();
    }

    internal Int16 GetInt16(int i)
    {
      Sync(i);
      if (_keyInfo[i].query != null) return _keyInfo[i].query._reader.GetInt16(_keyInfo[i].column);
      else
      {
        long rowid = _stmt._sql.GetRowIdForCursor(_stmt, _keyInfo[i].cursor);
        if (rowid == 0) throw new InvalidCastException();
        return Convert.ToInt16(rowid);
      }
    }

    internal Int32 GetInt32(int i)
    {
      Sync(i);
      if (_keyInfo[i].query != null) return _keyInfo[i].query._reader.GetInt32(_keyInfo[i].column);
      else
      {
        long rowid = _stmt._sql.GetRowIdForCursor(_stmt, _keyInfo[i].cursor);
        if (rowid == 0) throw new InvalidCastException();
        return Convert.ToInt32(rowid);
      }
    }

    internal Int64 GetInt64(int i)
    {
      Sync(i);
      if (_keyInfo[i].query != null) return _keyInfo[i].query._reader.GetInt64(_keyInfo[i].column);
      else
      {
        long rowid = _stmt._sql.GetRowIdForCursor(_stmt, _keyInfo[i].cursor);
        if (rowid == 0) throw new InvalidCastException();
        return rowid;
      }
    }

    internal string GetString(int i)
    {
      Sync(i);
      if (_keyInfo[i].query != null) return _keyInfo[i].query._reader.GetString(_keyInfo[i].column);
      else throw new InvalidCastException();
    }

    internal object GetValue(int i)
    {
      if (_keyInfo[i].cursor == -1) return DBNull.Value;

      Sync(i);
      if (_keyInfo[i].query != null) return _keyInfo[i].query._reader.GetValue(_keyInfo[i].column);

      if (IsDBNull(i) == true)
        return DBNull.Value;
      else return GetInt64(i);
    }

    internal bool IsDBNull(int i)
    {
      if (_keyInfo[i].cursor == -1) return true;

      Sync(i);
      if (_keyInfo[i].query != null) return _keyInfo[i].query._reader.IsDBNull(_keyInfo[i].column);
      else return _stmt._sql.GetRowIdForCursor(_stmt, _keyInfo[i].cursor) == 0;
    }

    /// <summary>
    /// Append all the columns we've added to the original query to the schema
    /// </summary>
    /// <param name="tbl"></param>
    internal void AppendSchemaTable(DataTable tbl)
    {
      KeyQuery last = null;

      for (int n = 0; n < _keyInfo.Length; n++)
      {
        if (_keyInfo[n].query == null || _keyInfo[n].query != last)
        {
          last = _keyInfo[n].query;

          if (last == null) // ROWID aliases are treated special
          {
            DataRow row = tbl.NewRow();
            row[SchemaTableColumn.ColumnName] = _keyInfo[n].columnName;
            row[SchemaTableColumn.ColumnOrdinal] = tbl.Rows.Count;
            row[SchemaTableColumn.ColumnSize] = 8;
            row[SchemaTableColumn.NumericPrecision] = 255;
            row[SchemaTableColumn.NumericScale] = 255;
            row[SchemaTableColumn.ProviderType] = DbType.Int64;
            row[SchemaTableColumn.IsLong] = false;
            row[SchemaTableColumn.AllowDBNull] = false;
            row[SchemaTableOptionalColumn.IsReadOnly] = false;
            row[SchemaTableOptionalColumn.IsRowVersion] = false;
            row[SchemaTableColumn.IsUnique] = false;
            row[SchemaTableColumn.IsKey] = true;
            row[SchemaTableColumn.DataType] = typeof(Int64);
            row[SchemaTableOptionalColumn.IsHidden] = true;
            row[SchemaTableColumn.BaseColumnName] = _keyInfo[n].columnName;
            row[SchemaTableColumn.IsExpression] = false;
            row[SchemaTableColumn.IsAliased] = false;
            row[SchemaTableColumn.BaseTableName] = _keyInfo[n].tableName;
            row[SchemaTableOptionalColumn.BaseCatalogName] = _keyInfo[n].databaseName;
            row[SchemaTableOptionalColumn.IsAutoIncrement] = true;
            row["DataTypeName"] = "integer";

            tbl.Rows.Add(row);
          }
          else
          {
            last.Sync(0);
            using (DataTable tblSub = last._reader.GetSchemaTable())
            {
              foreach (DataRow row in tblSub.Rows)
              {
                object[] o = row.ItemArray;
                DataRow newrow = tbl.Rows.Add(o);
                newrow[SchemaTableOptionalColumn.IsHidden] = true;
                newrow[SchemaTableColumn.ColumnOrdinal] = tbl.Rows.Count - 1;
              }
            }
          }
        }
      }
    }
  }
}
