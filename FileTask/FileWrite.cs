using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


public interface IWriteStrat<T>
{
    Task WriteAsync(string path, T data);
}

public class FileStrat : IWriteStrat<string>
{
    public async Task WriteAsync(string path, string data)
    {
        File.WriteAllText(path, data);
        Console.WriteLine("Записано через File.Write");
        await Task.CompletedTask;
    }
}

public class StreamStrat : IWriteStrat<string>
{
    public async Task WriteAsync(string path, string data)
    {
        using (StreamWriter writer = new StreamWriter(path))
        {
            writer.Write(data);
        }
        Console.WriteLine("Запись через Stream");
        await Task.CompletedTask;
    }
}


public class StreamAwaitStrat : IWriteStrat<string>
{
    public async Task WriteAsync(string path, string data)
    {
        using (StreamWriter reader = new StreamWriter(path))
        {
            await reader.WriteAsync(data);

        }
        Console.WriteLine("Запись черезз Stream ассинхронно");
    }
}

public class SetStrategy<T>
{
    private IWriteStrat<T> strat;

    public void Set(IWriteStrat<T> strat)
    {
        this.strat = strat;
    }

    public async Task Start(string path, T data)
    {
        await strat.WriteAsync(path, data);
    }
}
namespace FileReadWrite
{


    internal class Program
    {
        static async Task Main(string[] args)
        {
            int random = 0;
            Random rnd = new Random();
            string filePath = @"C:\test\file.txt";

            Console.WriteLine("Введите строку которую хотите ввести:");
            string input = Console.ReadLine();

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
            await setStrategy.Start(filePath, input);
        }
    }