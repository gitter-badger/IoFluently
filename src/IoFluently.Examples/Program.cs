﻿using System;
using System.Linq;
using System.Text.RegularExpressions;
using LiveLinq;
using LiveLinq.Core;
using ReactiveProcesses;

namespace IoFluently.Examples
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            var service = new IoService(new ReactiveProcessFactory());
            
            var repositoryRoot = service.CurrentDirectory.Ancestors().First(ancestor => (ancestor / ".git").Exists());

            var todoRegex = new Regex(@"TODO:(?<todoDescription>[^\n]+)");
            
            var changes = repositoryRoot.Descendants()
                .ToLiveLinq()
                .Where(path => path.HasExtension(".md") && path.GetPathType() == PathType.File)
                .Select(path => path.AsTextFile().Read().Lines
                    .Select(line => todoRegex.Match(line))
                    .Where(match => match.Success)
                    .Select(match => new { match, path }))
                .SelectMany(x => x);

            changes.AsObservable().Subscribe(x =>
            {
                if (x.Type == CollectionChangeType.Add)
                {
                    foreach (var item in x.Values)
                    {
                        Console.WriteLine($"A TODO item was added: {item.match.Groups["todoDescription"].Value.Trim()}");
                    }
                }
                else
                {
                    foreach (var item in x.Values)
                    {
                        Console.WriteLine($"A TODO item was removed: {item.match.Groups["todoDescription"].Value.Trim()}");
                    }
                }
            });

            Console.WriteLine("Press any key to exit");
            Console.ReadKey();
        }
    }
}