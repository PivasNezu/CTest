﻿using System;
using Newtonsoft.Json;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;
using StackExchange.Redis;
using System.Runtime.Remoting.Messaging;
using System.Text.Json.Serialization;

namespace JsonReader
{
    public interface IReadJsonStrategy
    {
        Task<User[]> ReadAsync(string path);
    }
    public class FileJsonStrategy : IReadJsonStrategy
    {
        public async Task<User[]> ReadAsync(string path)
        {
            using (StreamReader reader = new StreamReader(path))
            {
                string content = await reader.ReadToEndAsync();
                Console.WriteLine("Прочитано через StreamReader.");

                
                User[] users = JsonConvert.DeserializeObject<User[]>(content);
                return users;  
            }
        }
    }


    public class WorkPostgres
    {
        private const string Connection = "Host=localhost;Port=5438;Username=postgres;Password=12345;Database=postgres";

        public async Task CreateTable()
        {
            using (var connection = new NpgsqlConnection(Connection))
            {
                await connection.OpenAsync();
                var command = new NpgsqlCommand(@"CREATE TABLE IF NOT EXISTS Users (
                    Id SERIAL PRIMARY KEY,
                    Name TEXT NOT NULL,
                    Email TEXT NOT NULL,
                    Age INT,
                    IsActive BOOLEAN
                )", connection);
                await command.ExecuteNonQueryAsync();
            }
        }

        public async Task InsertToDB(int id, string name, string email, int age, bool isActive)
        {
            using (var connection = new NpgsqlConnection(Connection))
            {
                await connection.OpenAsync();
                var command = new NpgsqlCommand(
                    @"INSERT INTO Users (Id, Name, Email, Age, IsActive) VALUES (@id, @name, @email, @age, @isActive)",
                    connection);
                command.Parameters.AddWithValue("id", id);
                command.Parameters.AddWithValue("name", name);
                command.Parameters.AddWithValue("email", email);
                command.Parameters.AddWithValue("age", age);
                command.Parameters.AddWithValue("isActive", isActive);
                await command.ExecuteNonQueryAsync();
            }
        }
    }

    public class Redis
    {
        private ConnectionMultiplexer redis = ConnectionMultiplexer.Connect("localhost:6379");
        private IDatabase db;

        public Redis()
        {
            db = redis.GetDatabase();
        }

        public async Task SetData(string key, string value)
        {
            await db.StringSetAsync(key, value);
        }

        public async Task<string> GetData(string key)
        {
            return await db.StringGetAsync(key);
        }
    }

    public class DelegateForJSON
    {
        public event Func<User[], Task> OnDataRead;

        public async Task Read(string path, IReadJsonStrategy strat)
        {
            User[] users = await strat.ReadAsync(path);  
            if (OnDataRead != null)
                await OnDataRead(users);  
        }
    }

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public int Age { get; set; }
        public bool IsActive { get; set; }
    }


    internal class Program
    {
        
        static async Task Main(string[] args)
        {

            Console.WriteLine("Введите путь к JSON файлу:");
            string filePath = Console.ReadLine();

            var postgres = new WorkPostgres();
            var redis = new Redis();

            await postgres.CreateTable();
            try
            {
                var reader = new DelegateForJSON();
                reader.OnDataRead += async (users) =>
                {
                    foreach (var user in users)
                    {
                        await postgres.InsertToDB(user.Id, user.Name, user.Email, user.Age, user.IsActive);
                        await redis.SetData("Last", JsonConvert.SerializeObject(user));
                    }
                    Console.WriteLine("Данные успешно записаны в PostgreSQL и Redis.");
                };

                IReadJsonStrategy strat = new FileJsonStrategy();

                await reader.Read(filePath, strat);

                string lastUser = await redis.GetData("Last");

                Console.WriteLine($"Последняя запись в Redis: {lastUser}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString() + "произошла ошибка");
            }

        }
    }
}
