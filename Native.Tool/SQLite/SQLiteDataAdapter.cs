/********************************************************
 * ADO.NET 2.0 Data Provider for SQLite Version 3.X
 * Written by Robert Simpson (robert@blackcastlesoft.com)
 * 
 * Released to the public domain, use at your own risk!
 ********************************************************/

namespace System.Data.SQLite
{
  using System;
  using System.Data.Common;
  using System.ComponentModel;

  /// <summary>
  /// SQLite implementation of DbDataAdapter.
  /// </summary>
#if !PLATFORM_COMPACTFRAMEWORK
  [DefaultEvent("RowUpdated")]
  [ToolboxItem("SQLite.Designer.SQLiteDataAdapterToolboxItem, SQLite.Designer, Version=" + SQLite3.DesignerVersion + ", Culture=neutral, PublicKeyToken=db937bc2d44ff139")]
  [Designer("Microsoft.VSDesigner.Data.VS.SqlDataAdapterDesigner, Microsoft.VSDesigner, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
#endif
  public sealed class SQLiteDataAdapter : DbDataAdapter
  {
    private bool disposeSelect = true;

    ///////////////////////////////////////////////////////////////////////////////////////////////

    private static object _updatingEventPH = new object();
    private static object _updatedEventPH = new object();

    ///////////////////////////////////////////////////////////////////////////////////////////////

    #region Public Constructors
    /// <overloads>
    /// This class is just a shell around the DbDataAdapter.  Nothing from
    /// DbDataAdapter is overridden here, just a few constructors are defined.
    /// </overloads>
    /// <summary>
    /// Default constructor.
    /// </summary>
    public SQLiteDataAdapter()
    {
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructs a data adapter using the specified select command.
    /// </summary>
    /// <param name="cmd">
    /// The select command to associate with the adapter.
    /// </param>
    public SQLiteDataAdapter(SQLiteCommand cmd)
    {
      SelectCommand = cmd;
      disposeSelect = false;
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructs a data adapter with the supplied select command text and
    /// associated with the specified connection.
    /// </summary>
    /// <param name="commandText">
    /// The select command text to associate with the data adapter.
    /// </param>
    /// <param name="connection">
    /// The connection to associate with the select command.
    /// </param>
    public SQLiteDataAdapter(string commandText, SQLiteConnection connection)
    {
      SelectCommand = new SQLiteCommand(commandText, connection);
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructs a data adapter with the specified select command text,
    /// and using the specified database connection string.
    /// </summary>
    /// <param name="commandText">
    /// The select command text to use to construct a select command.
    /// </param>
    /// <param name="connectionString">
    /// A connection string suitable for passing to a new SQLiteConnection,
    /// which is associated with the select command.
    /// </param>
    public SQLiteDataAdapter(
        string commandText,
        string connectionString
        )
        : this(commandText, connectionString, false)
    {
        // do nothing.
    }

    ///////////////////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// Constructs a data adapter with the specified select command text,
    /// and using the specified database connection string.
    /// </summary>
    /// <param name="commandText">
    /// The select command text to use to construct a select command.
    /// </param>
    /// <param name="connectionString">
    /// A connection string suitable for passing to a new SQLiteConnection,
    /// which is associated with the select command.
    /// </param>
    /// <param name="parseViaFramework">
    /// Non-zero to parse the connection string using the built-in (i.e.
    /// framework provided) parser when opening the connection.
    /// </param>
    public SQLiteDataAdapter(
        string commandText,
        string connectionString,
        bool parseViaFramework
        )
    {
      SQLiteConnection cnn = new SQLiteConnection(
          connectionString, parseViaFramework);

      SelectCommand = new SQLiteCommand(commandText, cnn);
    }
    #endregion

    ///////////////////////////////////////////////////////////////////////////////////////////////

    #region IDisposable "Pattern" Members
    private bool disposed;
    private void CheckDisposed() /* throw */
    {
#if THROW_ON_DISPOSED
        if (disposed)
            throw new ObjectDisposedException(typeof(SQLiteDataAdapter).Name);
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
                if (disposing)
                {
                    ////////////////////////////////////
                    // dispose managed resources here...
                    ////////////////////////////////////

                    if (disposeSelect && (SelectCommand != null))
                    {
                        SelectCommand.Dispose();
                        SelectCommand = null;
                    }

                    if (InsertCommand != null)
                    {
                        InsertCommand.Dispose();
                        InsertCommand = null;
                    }

                    if (UpdateCommand != null)
                    {
                        UpdateCommand.Dispose();
                        UpdateCommand = null;
                    }

                    if (DeleteCommand != null)
                    {
                        DeleteCommand.Dispose();
                        DeleteCommand = null;
                    }
                }

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
    /// Row updating event handler
    /// </summary>
    public event EventHandler<RowUpdatingEventArgs> RowUpdating
    {
      add
      {
        CheckDisposed();

#if !PLATFORM_COMPACTFRAMEWORK
        EventHandler<RowUpdatingEventArgs> previous = (EventHandler<RowUpdatingEventArgs>)base.Events[_updatingEventPH];
        if ((previous != null) && (value.Target is DbCommandBuilder))
        {
          EventHandler<RowUpdatingEventArgs> handler = (EventHandler<RowUpdatingEventArgs>)FindBuilder(previous);
          if (handler != null)
          {
            base.Events.RemoveHandler(_updatingEventPH, handler);
          }
        }
#endif
        base.Events.AddHandler(_updatingEventPH, value); 
      }
      remove { CheckDisposed(); base.Events.RemoveHandler(_updatingEventPH, value); }
    }

#if !PLATFORM_COMPACTFRAMEWORK
    internal static Delegate FindBuilder(MulticastDelegate mcd)
    {
      if (mcd != null)
      {
        Delegate[] invocationList = mcd.GetInvocationList();
        for (int i = 0; i < invocationList.Length; i++)
        {
          if (invocationList[i].Target is DbCommandBuilder)
          {
            return invocationList[i];
          }
        }
      }
      return null;
    }
#endif

    /// <summary>
    /// Row updated event handler
    /// </summary>
    public event EventHandler<RowUpdatedEventArgs> RowUpdated
    {
      add { CheckDisposed(); base.Events.AddHandler(_updatedEventPH, value); }
      remove { CheckDisposed(); base.Events.RemoveHandler(_updatedEventPH, value); }
    }

    /// <summary>
    /// Raised by the underlying DbDataAdapter when a row is being updated
    /// </summary>
    /// <param name="value">The event's specifics</param>
    protected override void OnRowUpdating(RowUpdatingEventArgs value)
    {
      EventHandler<RowUpdatingEventArgs> handler = base.Events[_updatingEventPH] as EventHandler<RowUpdatingEventArgs>;

      if (handler != null)
        handler(this, value);
    }

    /// <summary>
    /// Raised by DbDataAdapter after a row is updated
    /// </summary>
    /// <param name="value">The event's specifics</param>
    protected override void OnRowUpdated(RowUpdatedEventArgs value)
    {
      EventHandler<RowUpdatedEventArgs> handler = base.Events[_updatedEventPH] as EventHandler<RowUpdatedEventArgs>;

      if (handler != null)
        handler(this, value);
    }

    /// <summary>
    /// Gets/sets the select command for this DataAdapter
    /// </summary>
#if !PLATFORM_COMPACTFRAMEWORK
    [DefaultValue((string)null), Editor("Microsoft.VSDesigner.Data.Design.DBCommandEditor, Microsoft.VSDesigner, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", "System.Drawing.Design.UITypeEditor, System.Drawing, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
#endif
    public new SQLiteCommand SelectCommand
    {
      get { CheckDisposed(); return (SQLiteCommand)base.SelectCommand; }
      set { CheckDisposed(); base.SelectCommand = value; }
    }

    /// <summary>
    /// Gets/sets the insert command for this DataAdapter
    /// </summary>
#if !PLATFORM_COMPACTFRAMEWORK
    [DefaultValue((string)null), Editor("Microsoft.VSDesigner.Data.Design.DBCommandEditor, Microsoft.VSDesigner, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", "System.Drawing.Design.UITypeEditor, System.Drawing, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
#endif
    public new SQLiteCommand InsertCommand
    {
      get { CheckDisposed(); return (SQLiteCommand)base.InsertCommand; }
      set { CheckDisposed(); base.InsertCommand = value; }
    }

    /// <summary>
    /// Gets/sets the update command for this DataAdapter
    /// </summary>
#if !PLATFORM_COMPACTFRAMEWORK
    [DefaultValue((string)null), Editor("Microsoft.VSDesigner.Data.Design.DBCommandEditor, Microsoft.VSDesigner, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", "System.Drawing.Design.UITypeEditor, System.Drawing, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
#endif
    public new SQLiteCommand UpdateCommand
    {
      get { CheckDisposed(); return (SQLiteCommand)base.UpdateCommand; }
      set { CheckDisposed(); base.UpdateCommand = value; }
    }

    /// <summary>
    /// Gets/sets the delete command for this DataAdapter
    /// </summary>
#if !PLATFORM_COMPACTFRAMEWORK
    [DefaultValue((string)null), Editor("Microsoft.VSDesigner.Data.Design.DBCommandEditor, Microsoft.VSDesigner, Version=8.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", "System.Drawing.Design.UITypeEditor, System.Drawing, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
#endif
    public new SQLiteCommand DeleteCommand
    {
      get { CheckDisposed(); return (SQLiteCommand)base.DeleteCommand; }
      set { CheckDisposed(); base.DeleteCommand = value; }
    }
  }
}
