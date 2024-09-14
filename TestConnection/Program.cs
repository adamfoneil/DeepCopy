using Microsoft.Data.SqlClient;
using SampleApp;

using var cn = new SqlConnection(ApplicationDbContextFactory.GetConnectionString("master"));
cn.Open();
Console.WriteLine("connection opened");