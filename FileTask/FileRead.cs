using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public interface IReadStrat<T>
{
    Task<T> ReadAsync(string path);
}

public class FileStrat : IReadStrat<string>
{
    public async Task<string> ReadAsync(string path)
    {
        string data = File.ReadAllText(path);
        Console.WriteLine("Прочитано через File.Write");
        return await Task.FromResult(data);
    }
}

public class StreamStrat : IReadStrat<string>
{
    public async Task<string> ReadAsync(string path)
    {
        using (StreamReader read = new StreamReader(path))
        {
            string data = read.ReadToEnd();
            Console.WriteLine("Прочитано через Stream");
            return await Task.FromResult(data);
        }
    }
}



public class StreamAwaitStrat : IReadStrat<string>
{
    public async Task<string> ReadAsync(string path)
    {
        using (StreamReader read = new StreamReader(path))
        {
            string data = await read.ReadToEndAsync();
            Console.WriteLine("Прочитано через Stream (асинхронно)");
            return data;
        }
    }
}

public class SetStrategy<T>
{
    private IReadStrat<T> strat;

    public void Set(IReadStrat<T> strat)
    {
        this.strat = strat;
    }

    public async Task<T> Start(string path)
    {
        return await strat.ReadAsync(path);
    }
}
namespace FileRead
{


    internal class Program
    {
        static async Task Main(string[] args)
        {
            int random = 0;
            Random rnd = new Random();
            string filePath = "";

            Console.WriteLine("Введите адрес файла:");
            filePath = Console.ReadLine();

            random = rnd.Next(0, 3);
            var setStrategy = new SetStrategy<string>();

            switch (random)
            {
                case 0:
                    setStrategy.Set(new FileStrat());
                    break;
                case 1:
                    setStrategy.Set(new StreamStrat());
                    break;
                case 2:
                    setStrategy.Set(new StreamAwaitStrat());
                    break;
                default:
                    Console.WriteLine("Некорректный выбор.");
                    break;
            }
            string result = await setStrategy.Start(filePath);
            Console.WriteLine($"Результат чтения: \n {result}");
        }
    }
}
