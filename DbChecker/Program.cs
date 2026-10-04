
using Microsoft.Data.Sqlite;
using System;
class Program {
    static void Main() {
        using var connection = new SqliteConnection("Data Source=GradLink.db");
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = "SELECT Id, FullName, CvFilePath FROM AspNetUsers";
        using var reader = command.ExecuteReader();
        while (reader.Read()) {
            Console.WriteLine($"{reader["Id"]} | {reader["FullName"]} | {reader["CvFilePath"]}");
        }
    }
}

