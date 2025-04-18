using Congen.Storage.Business.Data_Objects.Requests;
using Congen.Storage.Data;
using Congen.Storage.Data.Data_Objects.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Congen.Storage.Business
{
    public class ServiceRepo : IServiceRepo
    {
        public List<Service> GetServiceFiles(int type = 0)
        {
            List<Service> services = new List<Service>();

            try
            {
                using (SqlConnection connection = new SqlConnection(Util.ConnectionString))
                {
                    try
                    {
                        SqlCommand cmd = new SqlCommand("con_GetServiceFiles", connection);
                        cmd.CommandType = System.Data.CommandType.StoredProcedure;

                        if (type > 0) cmd.Parameters.Add(new SqlParameter("Type", type));

                        if (connection.State == System.Data.ConnectionState.Closed)
                        {
                            connection.Open();
                        }

                        using (var dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                Service service = new Service();

                                service.Id = (int)dr["Id"];
                                service.Type = (int)dr["Type"];
                                service.Url = (string)dr["Url"];

                                if (dr["Title"] != DBNull.Value) service.Title = (string)dr["Title"];

                                if(dr["Description"] != DBNull.Value) service.Description = (string)dr["Description"];

                                services.Add(service);
                            }
                        }
                    }

                    catch (Exception ex)
                    {
                        throw ex;
                    }

                    finally
                    {
                        connection.Close();
                    }
                }
            }

            catch(Exception ex)
            {
                throw new Exception("An error occured while retrieving the service files.", ex);
            }

            return services;
        }
    }
}
