using MySqlConnector;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace StockGenius
{
    class Program
    {
        static string connectionString = "server=192.168.174.145;port=3306;database=ecom;user=root;password=elisa123";

        static async Task Main()
        {
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://localhost:8080/");
            listener.Start();

            Console.WriteLine("Serveur en cours d'exécution. Attente des requêtes...");

            while (true)
            {
                HttpListenerContext context = await listener.GetContextAsync();
                await ProcessRequestAsync(context);
            }
        }

        static async Task ProcessRequestAsync(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            try
            {
                string responseData = "";

                switch (request.HttpMethod)
                {
                    case "GET":
                        responseData = await HandleGetRequestAsync(request);
                        break;
                    case "POST":
                        responseData = await HandlePostRequestAsync(request);
                        break;
                    case "PUT":
                        responseData = await HandlePutRequestAsync(request);
                        break;
                    case "DELETE":
                        responseData = await HandleDeleteRequestAsync(request);
                        break;
                    default:
                        response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                        break;
                }

                byte[] buffer = Encoding.UTF8.GetBytes(responseData);
                response.ContentLength64 = buffer.Length;
                Stream output = response.OutputStream;
                await output.WriteAsync(buffer, 0, buffer.Length);
                output.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Une erreur est survenue: " + ex.Message);
                response.StatusCode = (int)HttpStatusCode.InternalServerError;
                response.Close();
            }
        }

        static async Task<string> HandleGetRequestAsync(HttpListenerRequest request)
        {
            if (request.Url.AbsolutePath == "/articles")
            {
                return await GetAllArticlesAsync();
            }
            else if (request.Url.AbsolutePath.StartsWith("/articles/"))
            {
                int id = GetIdFromPath(request.Url.AbsolutePath);
                return await GetArticleAsync(id);
            }
            else if (request.Url.AbsolutePath == "/users")
            {
                return await GetAllUsersAsync();
            }
            else if (request.Url.AbsolutePath.StartsWith("/users/"))
            {
                int id = GetIdFromPath(request.Url.AbsolutePath);
                return await GetUserAsync(id);
            }

            return "Endpoint non trouvé";
        }

        static async Task<string> HandlePostRequestAsync(HttpListenerRequest request)
        {
            if (request.Url.AbsolutePath == "/articles")
            {
                Article newArticle = await DeserializeAsync<Article>(request.InputStream);
                return await AddArticleAsync(newArticle);
            }
            else if (request.Url.AbsolutePath == "/users")
            {
                User newUser = await DeserializeAsync<User>(request.InputStream);
                return await AddUserAsync(newUser);
            }

            return "Endpoint non trouvé";
        }

        static async Task<string> HandlePutRequestAsync(HttpListenerRequest request)
        {
            if (request.Url.AbsolutePath.StartsWith("/articles/"))
            {
                int id = GetIdFromPath(request.Url.AbsolutePath);
                Article updatedArticle = await DeserializeAsync<Article>(request.InputStream);
                return await UpdateArticleAsync(id, updatedArticle);
            }
            else if (request.Url.AbsolutePath.StartsWith("/users/"))
            {
                int id = GetIdFromPath(request.Url.AbsolutePath);
                User updatedUser = await DeserializeAsync<User>(request.InputStream);
                return await UpdateUserAsync(id, updatedUser);
            }

            return "Endpoint non trouvé";
        }

        static async Task<string> HandleDeleteRequestAsync(HttpListenerRequest request)
        {
            if (request.Url.AbsolutePath.StartsWith("/articles/"))
            {
                int id = GetIdFromPath(request.Url.AbsolutePath);
                return await DeleteArticleAsync(id);
            }
            else if (request.Url.AbsolutePath.StartsWith("/users/"))
            {
                int id = GetIdFromPath(request.Url.AbsolutePath);
                return await DeleteUserAsync(id);
            }

            return "Endpoint non trouvé";
        }

        static async Task<string> GetAllArticlesAsync()
        {
            List<Article> articles = new List<Article>();
            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM Articles;";
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        articles.Add(new Article
                        {
                            Id = reader.GetInt32("Id"),
                            Title = reader.GetString("Title"),
                            Content = reader.GetString("Content")
                        });
                    }
                }
            }
            return SerializeAsync(articles);
        }

        static async Task<string> GetArticleAsync(int id)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM Articles WHERE Id = @id;";
                command.Parameters.AddWithValue("@id", id);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return SerializeAsync(new Article
                        {
                            Id = reader.GetInt32("Id"),
                            Title = reader.GetString("Title"),
                            Content = reader.GetString("Content")
                        });
                    }
                }
            }
            return "Article non trouvé";
        }

        static async Task<string> AddArticleAsync(Article article)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = "INSERT INTO Articles (Title, Content) VALUES (@title, @content);";
                command.Parameters.AddWithValue("@title", article.Title);
                command.Parameters.AddWithValue("@content", article.Content);
                await command.ExecuteNonQueryAsync();
                return $"Article ajouté avec l'ID {command.LastInsertedId}";
            }
        }

        static async Task<string> UpdateArticleAsync(int id, Article article)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = "UPDATE Articles SET Title = @title, Content = @content WHERE Id = @id;";
                command.Parameters.AddWithValue("@id", id);
                command.Parameters.AddWithValue("@title", article.Title);
                command.Parameters.AddWithValue("@content", article.Content);
                int rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0 ? "Article mis à jour avec succès" : "Article non trouvé";
            }
        }

        static async Task<string> DeleteArticleAsync(int id)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM Articles WHERE Id = @id;";
                command.Parameters.AddWithValue("@id", id);
                int rowsAffected = await command.ExecuteNonQueryAsync();

                if (rowsAffected > 0)
                {
                    await ReorderIdsAsync("Articles");
                    return "Article supprimé avec succès et IDs réordonnés";
                }
                else
                {
                    return "Article non trouvé";
                }
            }
        }

        static async Task<string> GetAllUsersAsync()
        {
            List<User> users = new List<User>();
            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM Users;";
                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        users.Add(new User
                        {
                            Id = reader.GetInt32("Id"),
                            Username = reader.GetString("Username"),
                            Email = reader.GetString("Email"),
                            Password = reader.GetString("Password")
                        });
                    }
                }
            }
            return SerializeAsync(users);
        }

        static async Task<string> GetUserAsync(int id)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = "SELECT * FROM Users WHERE Id = @id;";
                command.Parameters.AddWithValue("@id", id);

                using (var reader = await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        return SerializeAsync(new User
                        {
                            Id = reader.GetInt32("Id"),
                            Username = reader.GetString("Username"),
                            Email = reader.GetString("Email"),
                            Password = reader.GetString("Password")
                        });
                    }
                }
            }
            return "Utilisateur non trouvé";
        }

        static async Task<string> AddUserAsync(User user)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = "INSERT INTO Users (Username, Email, Password) VALUES (@username, @email, @password);";
                command.Parameters.AddWithValue("@username", user.Username);
                command.Parameters.AddWithValue("@email", user.Email);
                command.Parameters.AddWithValue("@password", user.Password);
                await command.ExecuteNonQueryAsync();
                return $"Utilisateur ajouté avec l'ID {command.LastInsertedId}";
            }
        }

        static async Task<string> UpdateUserAsync(int id, User user)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = "UPDATE Users SET Username = @username, Email = @email, Password = @password WHERE Id = @id;";
                command.Parameters.AddWithValue("@id", id);
                command.Parameters.AddWithValue("@username", user.Username);
                command.Parameters.AddWithValue("@email", user.Email);
                command.Parameters.AddWithValue("@password", user.Password);
                int rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0 ? "Utilisateur mis à jour avec succès" : "Utilisateur non trouvé";
            }
        }

        static async Task<string> DeleteUserAsync(int id)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                command.CommandText = "DELETE FROM Users WHERE Id = @id;";
                command.Parameters.AddWithValue("@id", id);
                int rowsAffected = await command.ExecuteNonQueryAsync();

                if (rowsAffected > 0)
                {
                    await ReorderIdsAsync("Users");
                    return "Utilisateur supprimé avec succès et IDs réordonnés";
                }
                else
                {
                    return "Utilisateur non trouvé";
                }
            }
        }

        static string SerializeAsync<T>(T obj)
        {
            return JsonSerializer.Serialize(obj);
        }

        static async Task<T> DeserializeAsync<T>(Stream inputStream)
        {
            using (StreamReader reader = new StreamReader(inputStream))
            {
                string json = await reader.ReadToEndAsync();
                return JsonSerializer.Deserialize<T>(json);
            }
        }

        static async Task ReorderIdsAsync(string tableName)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                await connection.OpenAsync();
                var command = connection.CreateCommand();
                // Réorganiser les IDs en utilisant la fonction ROW_NUMBER
                command.CommandText = $"UPDATE {tableName} AS t1 JOIN (SELECT Id, ROW_NUMBER() OVER (ORDER BY Id) AS newId FROM {tableName}) AS t2 ON t1.Id = t2.Id SET t1.Id = t2.newId;";
                await command.ExecuteNonQueryAsync();
                // Réinitialiser la séquence d'auto-incrémentation
                command.CommandText = $"ALTER TABLE {tableName} AUTO_INCREMENT = 1;";
                await command.ExecuteNonQueryAsync();
            }
        }

        static int GetIdFromPath(string path)
        {
            string[] parts = path.Split('/');
            return parts.Length > 2 && int.TryParse(parts[2], out int id) ? id : -1;
        }
    }

   
}
