using SQLitePCL;
namespace BRM_2;

public partial class SQLiteConnection 
{
	private static bool _initialized;
	private static readonly object _initLock = new object();

	/// <summary>
	/// Ensures SQLitePCLRaw is initialized before any database operations.
	/// Safe to call multiple times; initialization only occurs once.
	/// </summary>
	internal static void EnsureInitialized()
	{
		if (_initialized)
			return;

		lock (_initLock)
		{
			if (_initialized)
				return;

			try
			{
				Batteries_V2.Init();
				_initialized = true;
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException(
					"Failed to initialize SQLitePCLRaw. Ensure SQLitePCLRaw.batteries_v2.dll and provider assemblies are properly bundled.",
					ex);
			}
		}
	}


    /// <summary>
    /// Constructor initializer that ensures SQLitePCLRaw is set up before creating any connection.
    /// </summary>
    private void InitializeConnection()
    {
        EnsureInitialized();
    }
}