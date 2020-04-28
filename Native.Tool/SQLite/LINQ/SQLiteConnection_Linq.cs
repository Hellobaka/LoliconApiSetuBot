/********************************************************
 * ADO.NET 2.0 Data Provider for SQLite Version 3.X
 * Written by Robert Simpson (robert@blackcastlesoft.com)
 * 
 * Released to the public domain, use at your own risk!
 ********************************************************/

namespace System.Data.SQLite
{
  using System.Data.Common;

  public sealed partial class SQLiteConnection
  {
    /// <summary>
    /// Returns the <see cref="SQLiteFactory" /> instance.
    /// </summary>
    protected override DbProviderFactory DbProviderFactory
    {
      get { return SQLiteFactory.Instance; }
    }
  }
}

