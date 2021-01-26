using Mono.Data.Sqlite;
using System.Data;

public static class DB
{

    //Set up variables for the database connection
    private static IDbConnection conn;
    private static string path;

    //variable that holds the results of the query
    public static IDataReader reader;

    public static void Connect(string dbPath)
    {
        //sets the private path from the path given
        path = dbPath;
        OpenDB();
    }

    public static void OpenDB()
    {
        //set up the connection
        conn = new SqliteConnection(path);
        //Open the connection to the database
        conn.Open();
    }

    public static void CloseDB()
    {
        //clean up (empty) the reader if it is open
        if (reader != null)
        {
            reader.Close();
            reader = null;
        }
        //close the connection
        conn.Close();
        conn = null;
    }

    public static void RunQuery(string query)
    {
        //make sure the database connection is set up
        if (path == null)
        {
            throw new System.Exception("DB.Connect() must be successfully run before DB.RunQuery()");
        }
        //conect to the database if the connection is not open
        if (conn == null)
        {
            OpenDB();
        }

        //create a command for the database
        IDbCommand cmd = conn.CreateCommand();
        //set the command query
        cmd.CommandText = query;
        //run the query and prepare the reader for data retrieval
        reader = cmd.ExecuteReader();
    }
}
