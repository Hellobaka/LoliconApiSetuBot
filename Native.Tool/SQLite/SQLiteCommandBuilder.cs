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
  using System.Globalization;
  using System.ComponentModel;

  /// <summary>
  /// SQLite implementation of DbCommandBuilder.
  /// </summary>
  public sealed class SQLiteCommandBuilder : DbCommandBuilder
  {
    /// <summary>
    /// Default constructor
    /// </summary>
    public SQLiteCommandBuilder() : this(null)
    {
    }

    /// <summary>
    /// Initializes the command builder and associates it with the specified data adapter.
    /// </summary>
    /// <param name="adp"></param>
    public SQLiteCommandBuilder(SQLiteDataAdapter adp)
    {
      QuotePrefix = "[";
      QuoteSuffix = "]";
      DataAdapter = adp;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    #region IDisposable "Pattern" Members
    private bool disposed;
    private void CheckDisposed() /* throw */
    {
#if THROW_ON_DISPOSED
        if (disposed)
            throw new ObjectDisposedException(typeof(SQLiteCommandBuilder).Name);
#endif
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Cleans up resources (native and managed) associated with the current instance.
    /// </summary>
    /// <param name="disposing">
    /// Zero when being disposed via garbage collection; otherwise, non-zero.
    /// </param>
    protected override void Dispose(bool disposing)
    {
        try
        {
            if (!disposed)
            {
                //if (disposing)
                //{
                //    ////////////////////////////////////
                //    // dispose managed resources here...
                //    ////////////////////////////////////
                //}

                //////////////////////////////////////
                // release unmanaged resources here...
                //////////////////////////////////////
            }
        }
        finally
        {
            base.Dispose(disposing);

            //
            // NOTE: Everything should be fully disposed at this point.
            //
            disposed = true;
        }
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Minimal amount of parameter processing.  Primarily sets the DbType for the parameter equal to the provider type in the schema
    /// </summary>
    /// <param name="parameter">The parameter to use in applying custom behaviors to a row</param>
    /// <param name="row">The row to apply the parameter to</param>
    /// <param name="statementType">The type of statement</param>
    /// <param name="whereClause">Whether the application of the parameter is part of a WHERE clause</param>
    protected override void ApplyParameterInfo(DbParameter parameter, DataRow row, StatementType statementType, bool whereClause)
    {
      SQLiteParameter param = (SQLiteParameter)parameter;
      param.DbType = (DbType)row[SchemaTableColumn.ProviderType];
    }

    /// <summary>
    /// Returns a valid named parameter
    /// </summary>
    /// <param name="parameterName">The name of the parameter</param>
    /// <returns>Error</returns>
    protected override string GetParameterName(string parameterName)
    {
      return HelperMethods.StringFormat(CultureInfo.InvariantCulture, "@{0}", parameterName);
    }

    /// <summary>
    /// Returns a named parameter for the given ordinal
    /// </summary>
    /// <param name="parameterOrdinal">The i of the parameter</param>
    /// <returns>Error</returns>
    protected override string GetParameterName(int parameterOrdinal)
    {
      return HelperMethods.StringFormat(CultureInfo.InvariantCulture, "@param{0}", parameterOrdinal);
    }

    /// <summary>
    /// Returns a placeholder character for the specified parameter i.
    /// </summary>
    /// <param name="parameterOrdinal">The index of the parameter to provide a placeholder for</param>
    /// <returns>Returns a named parameter</returns>
    protected override string GetParameterPlaceholder(int parameterOrdinal)
    {
      return GetParameterName(parameterOrdinal);
    }

    /// <summary>
    /// Sets the handler for receiving row updating events.  Used by the DbCommandBuilder to autogenerate SQL
    /// statements that may not have previously been generated.
    /// </summary>
    /// <param name="adapter">A data adapter to receive events on.</param>
    protected override void SetRowUpdatingHandler(DbDataAdapter adapter)
    {
      if (adapter == base.DataAdapter)
      {
        ((SQLiteDataAdapter)adapter).RowUpdating -= new EventHandler<RowUpdatingEventArgs>(RowUpdatingEventHandler);
      }
      else
      {
        ((SQLiteDataAdapter)adapter).RowUpdating += new EventHandler<RowUpdatingEventArgs>(RowUpdatingEventHandler);
      }
    }

    private void RowUpdatingEventHandler(object sender, RowUpdatingEventArgs e)
    {
      base.RowUpdatingHandler(e);
    }

    /// <summary>
    /// Gets/sets the DataAdapter for this CommandBuilder
    /// </summary>
    public new SQLiteDataAdapter DataAdapter
    {
      get { CheckDisposed(); return (SQLiteDataAdapter)base.DataAdapter; }
      set { CheckDisposed(); base.DataAdapter = value; }
    }

    /// <summary>
    /// Returns the automatically-generated SQLite command to delete rows from the database
    /// </summary>
    /// <returns></returns>
    public new SQLiteCommand GetDeleteCommand()
    {
      CheckDisposed();
      return (SQLiteCommand)base.GetDeleteCommand();
    }

    /// <summary>
    /// Returns the automatically-generated SQLite command to delete rows from the database
    /// </summary>
    /// <param name="useColumnsForParameterNames"></param>
    /// <returns></returns>
    public new SQLiteCommand GetDeleteCommand(bool useColumnsForParameterNames)
    {
      CheckDisposed();
      return (SQLiteCommand)base.GetDeleteCommand(useColumnsForParameterNames);
    }

    /// <summary>
    /// Returns the automatically-generated SQLite command to update rows in the database
    /// </summary>
    /// <returns></returns>
    public new SQLiteCommand GetUpdateCommand()
    {
      CheckDisposed();
      return (SQLiteCommand)base.GetUpdateCommand();
    }

    /// <summary>
    /// Returns the automatically-generated SQLite command to update rows in the database
    /// </summary>
    /// <param name="useColumnsForParameterNames"></param>
    /// <returns></returns>
    public new SQLiteCommand GetUpdateCommand(bool useColumnsForParameterNames)
    {
      CheckDisposed();
      return (SQLiteCommand)base.GetUpdateCommand(useColumnsForParameterNames);
    }

    /// <summary>
    /// Returns the automatically-generated SQLite command to insert rows into the database
    /// </summary>
    /// <returns></returns>
    public new SQLiteCommand GetInsertCommand()
    {
      CheckDisposed();
      return (SQLiteCommand)base.GetInsertCommand();
    }

    /// <summary>
    /// Returns the automatically-generated SQLite command to insert rows into the database
    /// </summary>
    /// <param name="useColumnsForParameterNames"></param>
    /// <returns></returns>
    public new SQLiteCommand GetInsertCommand(bool useColumnsForParameterNames)
    {
      CheckDisposed();
      return (SQLiteCommand)base.GetInsertCommand(useColumnsForParameterNames);
    }

    /// <summary>
    /// Overridden to hide its property from the designer
    /// </summary>
#if !PLATFORM_COMPACTFRAMEWORK
    [Browsable(false)]
#endif
    public override CatalogLocation CatalogLocation
    {
      get
      {
        CheckDisposed();
        return base.CatalogLocation;
      }
      set
      {
        CheckDisposed();
        base.CatalogLocation = value;
      }
    }

    /// <summary>
    /// Overridden to hide its property from the designer
    /// </summary>
#if !PLATFORM_COMPACTFRAMEWORK
    [Browsable(false)]
#endif
    public override string CatalogSeparator
    {
      get
      {
        CheckDisposed();
        return base.CatalogSeparator;
      }
      set
      {
        CheckDisposed();
        base.CatalogSeparator = value;
      }
    }

    /// <summary>
    /// Overridden to hide its property from the designer
    /// </summary>
#if !PLATFORM_COMPACTFRAMEWORK
    [Browsable(false)]
#endif
    [DefaultValue("[")]
    public override string QuotePrefix
    {
      get
      {
        CheckDisposed();
        return base.QuotePrefix;
      }
      set
      {
        CheckDisposed();
        base.QuotePrefix = value;
      }
    }

    /// <summary>
    /// Overridden to hide its property from the designer
    /// </summary>
#if !PLATFORM_COMPACTFRAMEWORK
    [Browsable(false)]
#endif
    public override string QuoteSuffix
    {
      get
      {
        CheckDisposed();
        return base.QuoteSuffix;
      }
      set
      {
        CheckDisposed();
        base.QuoteSuffix = value;
      }
    }

    /// <summary>
    /// Places brackets around an identifier
    /// </summary>
    /// <param name="unquotedIdentifier">The identifier to quote</param>
    /// <returns>The bracketed identifier</returns>
    public override string QuoteIdentifier(string unquotedIdentifier)
    {
      CheckDisposed();

      if (String.IsNullOrEmpty(QuotePrefix)
        || String.IsNullOrEmpty(QuoteSuffix)
        || String.IsNullOrEmpty(unquotedIdentifier))
        return unquotedIdentifier;

      return QuotePrefix + unquotedIdentifier.Replace(QuoteSuffix, QuoteSuffix + QuoteSuffix) + QuoteSuffix;
    }

    /// <summary>
    /// Removes brackets around an identifier
    /// </summary>
    /// <param name="quotedIdentifier">The quoted (bracketed) identifier</param>
    /// <returns>The undecorated identifier</returns>
    public override string UnquoteIdentifier(string quotedIdentifier)
    {
      CheckDisposed();

      if (String.IsNullOrEmpty(QuotePrefix)
        || String.IsNullOrEmpty(QuoteSuffix)
        || String.IsNullOrEmpty(quotedIdentifier))
        return quotedIdentifier;

      if (quotedIdentifier.StartsWith(QuotePrefix, StringComparison.OrdinalIgnoreCase) == false
        || quotedIdentifier.EndsWith(QuoteSuffix, StringComparison.OrdinalIgnoreCase) == false)
        return quotedIdentifier;

      return quotedIdentifier.Substring(QuotePrefix.Length, quotedIdentifier.Length - (QuotePrefix.Length + QuoteSuffix.Length)).Replace(QuoteSuffix + QuoteSuffix, QuoteSuffix);
    }

    /// <summary>
    /// Overridden to hide its property from the designer
    /// </summary>
#if !PLATFORM_COMPACTFRAMEWORK
    [Browsable(false)]
#endif
    public override string SchemaSeparator
    {
      get
      {
        CheckDisposed();
        return base.SchemaSeparator;
      }
      set
      {
        CheckDisposed();
        base.SchemaSeparator = value;
      }
    }

    /// <summary>
    /// Override helper, which can help the base command builder choose the right keys for the given query
    /// </summary>
    /// <param name="sourceCommand"></param>
    /// <returns></returns>
    protected override DataTable GetSchemaTable(DbCommand sourceCommand)
    {
      using (IDataReader reader = sourceCommand.ExecuteReader(CommandBehavior.KeyInfo | CommandBehavior.SchemaOnly))
      {
        DataTable schema = reader.GetSchemaTable();

        // If the query contains a primary key, turn off the IsUnique property
        // for all the non-key columns
        if (HasSchemaPrimaryKey(schema))
          ResetIsUniqueSchemaColumn(schema);

        // if table has no primary key we use unique columns as a fall back
        return schema;
      }
    }

    private bool HasSchemaPrimaryKey(DataTable schema)
    {
      DataColumn IsKeyColumn = schema.Columns[SchemaTableColumn.IsKey];

      foreach (DataRow schemaRow in schema.Rows)
      {
        if ((bool)schemaRow[IsKeyColumn] == true)
          return true;
      }

      return false;
    }

    private void ResetIsUniqueSchemaColumn(DataTable schema)
    {
      DataColumn IsUniqueColumn = schema.Columns[SchemaTableColumn.IsUnique];
      DataColumn IsKeyColumn = schema.Columns[SchemaTableColumn.IsKey];

      foreach (DataRow schemaRow in schema.Rows)
      {
        if ((bool)schemaRow[IsKeyColumn] == false)
          schemaRow[IsUniqueColumn] = false;
      }

      schema.AcceptChanges();
    }
  }
}
