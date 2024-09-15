using Microsoft.Data.SqlClient;
using SampleAppLocal;

using var cn = new SqlConnection(ApplicationDbContextFactory.GetConnectionString("master"));
cn.Open();
Console.WriteLine("connection opened");