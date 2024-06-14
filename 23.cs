using System;
using System.Collections.Generic;
using System.Linq;
using Npgsql;

namespace DailyPlannerApp
{
    class Program
    {
        static void Main(string[] args)
        {
            string connectionString = "Host=10.30.0.137;Port=5432;Username=gr631_kaaal;Password=Sh48F3kF;Database=gr631_kaaal";

            var planner = new DailyPlanner(connectionString);
            planner.Run();
        }
    }

    public class DailyPlanner
    {
        private readonly string _connectionString;
        private NpgsqlConnection _connection;
        private bool _loggedIn;
        private int _currentIdUser;

        public DailyPlanner(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void Run()
        {
            using (_connection = new NpgsqlConnection(_connectionString))
            {
                _connection.Open();

                while (true)
                {
                    Console.WriteLine("1. Регистрация\n" +
                                      "2. Авторизация\n" +
                                      "3. Выйти");
                    int choice;
                    if (!int.TryParse(Console.ReadLine(), out choice))
                    {
                        Console.WriteLine("Некорректный ввод");
                        continue;
                    }

                    switch (choice)
                    {
                        case 1:
                            Register();
                            break;
                        case 2:
                            if (Login())
                            {
                                ManageTasks();
                            }
                            else
                            {
                                Console.WriteLine("Неверное имя пользователя или пароль.");
                            }

                            break;
                        case 3:
                            return;
                        default:
                            Console.WriteLine("Некорректный ввод");
                            break;
                    }
                }
            }
        }

        private void ManageTasks()
        {
            while (true)
            {
                Console.WriteLine("1. Добавить задачу\n" +
                                  "2. Редактировать задачу\n" +
                                  "3. Удалить задачу\n" +
                                  "4. Показать задачи на сегодня\n" +
                                  "5. Показать задачи на завтра\n" +
                                  "6. Показать задачи на неделю\n" +
                                  "7. Показать все задачи\n" +
                                  "8. Показать невыполненные задачи\n" +
                                  "9. Показать выполненные задачи\n" +
                                  "10. Выйти");
                int taskChoice;
                if (!int.TryParse(Console.ReadLine(), out taskChoice))
                {
                    Console.WriteLine("Некорректный ввод");
                    continue;
                }

                switch (taskChoice)
                {
                    case 1:
                        AddTask(_currentIdUser);
                        break;
                    case 2:
                        EditTask(_currentIdUser);
                        break;
                    case 3:
                        DeleteTask(_currentIdUser);
                        break;
                    case 4:
                        ViewTasksToday(_currentIdUser);
                        break;
                    case 5:
                        ViewTasksTomorrow(_currentIdUser);
                        break;
                    case 6:
                        ViewTasksWeek(_currentIdUser);
                        break;
                    case 7:
                        ViewTasks(_currentIdUser);
                        break;
                    case 8:
                        ViewIncompleteTasks(_currentIdUser);
                        break;
                    case 9:
                        ViewCompletedTasks(_currentIdUser);
                        break;
                    case 10:
                        return;
                    default:
                        Console.WriteLine("Некорректный ввод");
                        break;
                }
            }
        }

        private bool Login()
        {
            Console.Write("Введите логин: ");
            string username = Console.ReadLine();
            Console.Write("Введите пароль: ");
            string password = Console.ReadLine();

            using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
            {
                connection.Open();
        
                string query = "SELECT \"ID_user\" FROM \"User\" WHERE \"Username\" = @Username AND \"Password\" = @Password";
                NpgsqlCommand command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@Username", username);
                command.Parameters.AddWithValue("@Password", password);
        
                using (NpgsqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        _loggedIn = true;
                        _currentIdUser = reader.GetInt32(0);
                        Console.WriteLine("Вход выполнен успешно!");
                        return true;
                    }
                    else
                    {
                        Console.WriteLine("Некорректный логин или пароль");
                        return false;
                    }
                }
            }
        }

        private void Register()
        {
            Console.Write("Введите логин: ");
            string username = Console.ReadLine();
            Console.Write("Введите пароль: ");
            string password = Console.ReadLine();

            using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
            {
                connection.Open();
                string query = "INSERT INTO \"User\" (\"Username\", \"Password\") VALUES (@Username, @Password)";
                NpgsqlCommand command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@Username", username);
                command.Parameters.AddWithValue("@Password", password);
                command.ExecuteNonQuery();
                Console.WriteLine("Регистрация выполнена!");
            }
        }

        private void AddTask(int idUser)
        {
            Console.Write("Введите название задачи: ");
            string nameTask = Console.ReadLine();
            Console.Write("Введите описание задачи(необязательно): ");
            string description = Console.ReadLine();
            Console.Write("Введите срок выполнения задачи (yyyy-MM-dd): ");
            string dueDate = Console.ReadLine();
            
            if (!DateTime.TryParse(dueDate, out DateTime parsedDueDate))
            {
                Console.WriteLine("Некорректный формат даты.");
                return;
            }

            try
            {
                using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
                {
                    connection.Open();
                    
                    string query = "INSERT INTO \"Task\" (\"NameTask\", \"Description\", \"DueDate\", \"IsCompleted\", \"ID_user\") VALUES (@NameTask, @Description, @DueDate, 'False', @ID_user)";
                    using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@NameTask", nameTask);
                        command.Parameters.AddWithValue("@Description", description ?? (object)DBNull.Value); 
                        command.Parameters.AddWithValue("@DueDate", parsedDueDate);
                        command.Parameters.AddWithValue("@ID_user", idUser);
                        command.ExecuteNonQuery();
                    }
                }

                Console.WriteLine("Задача добавлена!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Произошла ошибка: {ex.Message}");
            }
        }

        static void EditTask(int idUser)
        {
            Console.Write("Введите номер задачи: ");
            int numberTask = int.Parse(Console.ReadLine());
            Console.Write("Введите новое название задачи: ");
            string nameTask = Console.ReadLine();
            Console.Write("Введите новое описание(необязательно): ");
            string description = Console.ReadLine();
            Console.Write("Введите новый срок выполнения задачи (yyyy-MM-dd): ");
            string dueDate = Console.ReadLine();
            Console.Write("Вы хотите, чтобы задача была помечена как выполненная(y|n)?: ");
            string isCompleted = Console.ReadLine();
            if (isCompleted.ToLower() == "y")
            {
                MarkTaskCompleted(numberTask, idUser);
            }
            
            if (!DateTime.TryParse(dueDate, out DateTime parsedDueDate))
            {
                Console.WriteLine("Некорректный формат даты.");
                return;
            }

            using (NpgsqlConnection connection = new NpgsqlConnection("Host=10.30.0.137;Username=gr631_kaaal;Password=Sh48F3kF;Database=gr631_kaaal"))
            {
                connection.Open();
                
                string query = "UPDATE \"Task\" SET \"NameTask\" = @NameTask, \"Description\" = @Description, \"DueDate\" = @DueDate, \"IsCompleted\" = @IsCompleted WHERE \"NumberTask\" = @NumberTask AND \"ID_user\" = @ID_user";
                NpgsqlCommand command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@NameTask", nameTask);
                command.Parameters.AddWithValue("@Description", description ?? (object)DBNull.Value); 
                command.Parameters.AddWithValue("@DueDate", parsedDueDate);
                command.Parameters.AddWithValue("@IsCompleted", isCompleted);
                command.Parameters.AddWithValue("@NumberTask", numberTask);
                command.Parameters.AddWithValue("@ID_user", idUser);
                int rowsAffected = command.ExecuteNonQuery();
                
                if (rowsAffected > 0)
                {
                    Console.WriteLine("Задача изменена!");
                }

                else
                {
                    Console.WriteLine("Ошибка!");
                }
            }
        }

        static void DeleteTask(int idUser)
        {
            Console.Write("Введите номер задачи: ");
            int numberTask = int.Parse(Console.ReadLine());

            using (NpgsqlConnection connection = new NpgsqlConnection("Host=10.30.0.137;Username=gr631_kaaal;Password=Sh48F3kF;Database=gr631_kaaal"))
            {
                connection.Open();
                
                string query = "DELETE FROM \"Task\" WHERE \"NumberTask\" = @NumberTask AND \"ID_user\" = @ID_user";
                NpgsqlCommand command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@NumberTask", numberTask);
                command.Parameters.AddWithValue("@ID_user", idUser);
                int rowsAffected = command.ExecuteNonQuery();

                if (rowsAffected > 0)
                {
                    Console.WriteLine("Задача удалена!");
                }

                else
                {
                    Console.WriteLine("Ошибка!");
                }
            }
        }

        static void ViewTasksToday(int idUser)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection("Host=10.30.0.137;Username=gr631_kaaal;Password=Sh48F3kF;Database=gr631_kaaal"))
            {
                connection.Open();
                
                string query = "SELECT \"NumberTask\", \"NameTask\", \"Description\", \"DueDate\", \"IsCompleted\" FROM \"Task\" WHERE \"ID_user\" = @ID_user AND \"DueDate\" = CURRENT_DATE";
                NpgsqlCommand command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@ID_user", idUser);
                NpgsqlDataReader reader = command.ExecuteReader();

                Console.WriteLine("Задачи на сегодня:");
                while (reader.Read())
                {
                    int numberTask = reader.GetInt32(0);
                    string taskName = reader.GetString(1);
                    string taskDescription = reader.GetString(2);
                    DateTime dueDate = reader.GetDateTime(3);
                    bool isCompleted = reader.GetBoolean(4);
                    Console.WriteLine($"Номер: {numberTask}, Название: {taskName}, Описание: {taskDescription}, Срок выполнения: {dueDate.ToString("yyyy-MM-dd")}, Статус: {isCompleted}");
                }
            }
        }

        static void ViewTasksTomorrow(int idUser)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection("Host=10.30.0.137;Username=gr631_kaaal;Password=Sh48F3kF;Database=gr631_kaaal"))
            {
                connection.Open();
                string query = "SELECT \"NumberTask\", \"NameTask\", \"Description\", \"DueDate\", \"IsCompleted\" FROM \"Task\" WHERE \"ID_user\" = @ID_user AND \"DueDate\" = CURRENT_DATE + 1";
                NpgsqlCommand command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@ID_user", idUser);
                NpgsqlDataReader reader = command.ExecuteReader();

                Console.WriteLine("Задачи на завтра:");
                while (reader.Read())
                {
                    int numberTask = reader.GetInt32(0);
                    string taskName = reader.GetString(1);
                    string taskDescription = reader.GetString(2);
                    DateTime dueDate = reader.GetDateTime(3);
                    bool isCompleted = reader.GetBoolean(4);
                    Console.WriteLine($"Номер: {numberTask}, Название: {taskName}, Описание: {taskDescription}, Срок выполнения: {dueDate.ToString("yyyy-MM-dd")}, Статус: {isCompleted}");
                }
            }
        }

        static void ViewTasksWeek(int idUser)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection("Host=10.30.0.137;Username=gr631_kaaal;Password=Sh48F3kF;Database=gr631_kaaal"))
            {
                connection.Open();

                string query = "SELECT \"NumberTask\", \"NameTask\", \"Description\", \"DueDate\", \"IsCompleted\" FROM \"Task\" WHERE \"ID_user\" = @ID_user AND \"DueDate\" BETWEEN CURRENT_DATE AND CURRENT_DATE + 7 ";
                NpgsqlCommand command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@ID_user", idUser);
                NpgsqlDataReader reader = command.ExecuteReader();

                Console.WriteLine("Задачи на неделю:");
                while (reader.Read())
                {
                    int numberTask = reader.GetInt32(0);
                    string taskName = reader.GetString(1);
                    string taskDescription = reader.GetString(2);
                    DateTime dueDate = reader.GetDateTime(3);
                    bool isCompleted = reader.GetBoolean(4);
                    Console.WriteLine($"Номер: {numberTask}, Название: {taskName}, Описание: {taskDescription}, Срок выполнения: {dueDate.ToString("yyyy-MM-dd")}, Статус: {isCompleted}");
                }
            }
        }

        static void ViewIncompleteTasks(int idUser)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection("Host=10.30.0.137;Username=gr631_kaaal;Password=Sh48F3kF;Database=gr631_kaaal"))
            {
                connection.Open();

                string query = "SELECT \"ID_task\", \"NameTask\", \"Description\", \"DueDate\", \"IsCompleted\" FROM \"Task\" WHERE \"ID_user\" = @ID_user AND \"IsCompleted\" = 'False'";
                NpgsqlCommand command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@ID_user", idUser);
                NpgsqlDataReader reader = command.ExecuteReader();

                Console.WriteLine("Невыполненные задачи:");
                while (reader.Read())
                {
                    int numberTask = reader.GetInt32(0);
                    string taskName = reader.GetString(1);
                    string taskDescription = reader.GetString(2);
                    DateTime dueDate = reader.GetDateTime(3);
                    bool isCompleted = reader.GetBoolean(4);
                    Console.WriteLine($"Номер: {numberTask}, Название: {taskName}, Описание: {taskDescription}, Срок выполнения: {dueDate.ToString("yyyy-MM-dd")}, Статус: {isCompleted}");
                }
            }
        }

        static void ViewCompletedTasks(int idUser)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection("Host=10.30.0.137;Username=gr631_kaaal;Password=Sh48F3kF;Database=gr631_kaaal"))
            {
                connection.Open();

                string query = "SELECT \"ID_task\", \"NameTask\", \"Description\", \"DueDate\", \"IsCompleted\" FROM \"Task\" WHERE \"ID_user\" = @Id_user AND \"IsCompleted\" = 'True'";
                NpgsqlCommand command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@Id_user", idUser);
                NpgsqlDataReader reader = command.ExecuteReader();

                Console.WriteLine("Выполненные задачи:");
                while (reader.Read())
                {
                    int numberTask = reader.GetInt32(0);
                    string taskName = reader.GetString(1);
                    string taskDescription = reader.GetString(2);
                    DateTime dueDate = reader.GetDateTime(3);
                    bool isCompleted = reader.GetBoolean(4);
                    Console.WriteLine($"Номер: {numberTask}, Название: {taskName}, Описание: {taskDescription}, Срок выполнения: {dueDate.ToString("yyyy-MM-dd")}, Статус: {isCompleted}");
                }
            }
        }

        static void ViewTasks(int idUser)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection("Host=10.30.0.137;Username=gr631_kaaal;Password=Sh48F3kF;Database=gr631_kaaal"))
            {
                connection.Open();

                string query = "SELECT \"NumberTask\", \"NameTask\", \"Description\", \"DueDate\", \"IsCompleted\" FROM \"Task\" WHERE \"ID_user\" = @Id_user";
                NpgsqlCommand command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@Id_user", idUser);
                NpgsqlDataReader reader = command.ExecuteReader();

                Console.WriteLine("Все задачи:");
                while (reader.Read())
                {
                    int numberTask = reader.GetInt32(0);
                    string taskName = reader.GetString(1);
                    string taskDescription = reader.GetString(2);
                    DateTime dueDate = reader.GetDateTime(3);
                    bool isCompleted = reader.GetBoolean(4);
                    Console.WriteLine($"Номер: {numberTask}, Название: {taskName}, Описание: {taskDescription}, Срок выполнения: {dueDate.ToString("yyyy-MM-dd")}, Статус: {isCompleted}");
                }
            }
        }
        
        static void MarkTaskCompleted(int numberTask, int idUser)
        {
            using (NpgsqlConnection connection = new NpgsqlConnection("Host=10.30.0.137;Username=gr631_kaaal;Password=Sh48F3kF;Database=gr631_kaaal"))
            {
                connection.Open();
        
                string query = "UPDATE \"Task\" SET \"IsCompleted\" = true WHERE \"NumberTask\" = @NumberTask AND \"ID_user\" = @ID_user";
                NpgsqlCommand command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("@NumberTask", numberTask);
                command.Parameters.AddWithValue("@ID_user", idUser);
                int rowsAffected = command.ExecuteNonQuery();
        
                if (rowsAffected > 0)
                {
                    Console.WriteLine("Задача помечена как выполненная.");
                }
                
                else
                {
                    Console.WriteLine("Ошибка при пометке задачи как выполненной.");
                }
            }
        }
    }
}

