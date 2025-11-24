using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Avalonia.Controls;
using System.Text.RegularExpressions;
using System.Threading;
using Avalonia.Media;
using Avalonia.Threading;

namespace AvaloniaApplication1.Views;

public partial class MainWindow : Window
{ 
    List<(string username, string id)> players = new();
    private static readonly string _directoryPath = Path.GetFullPath(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), @"AppData\LocalLow\VRChat\VRChat"));
    readonly FileSystemWatcher _watcher = new(_directoryPath);
    string? currentLog = string.Empty;
    
    
    public MainWindow()
    {
        InitializeComponent();
        Opened += ( _,_ ) =>
        {
            if (TryGetPlatformHandle()?.Handle is { } h)
                User32Utils.SetLayeredAndTransparent(h);

            if (Screens.Primary is { } primaryScreen)
                Height = primaryScreen.WorkingArea.Height;
        };
        //wait for vrchat to open
        while (Process.GetProcessesByName("VRChat")[0] == null) Thread.Sleep(100);
        new Thread(WatchForLog){IsBackground = true}.Start();
        new Thread(ReadLog){IsBackground = true}.Start();
    }

    void WatchForLog()
    {
        Console.WriteLine("Started Watcher");
        _watcher.EnableRaisingEvents = true;
        _watcher.IncludeSubdirectories = false;
        _watcher.Filter = "output_log_*.txt";
        _watcher.Changed += (s, e) =>
        {
            if (currentLog != e.FullPath) 
            {
                currentLog = e.FullPath;
                Console.WriteLine(currentLog);
                
            }
        };
    }

    void ReadLog()
    {
        while (currentLog == String.Empty) Thread.Sleep(100);
        using var fs = new FileStream(currentLog, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var sr = new StreamReader(fs);
        fs.Seek(0, SeekOrigin.End);
        while (true)
        {
            while (sr.ReadLine() is { } line)
            {
                ParseLine(line);
            }
            if (currentLog != fs.Name) { sr.Close(); fs.Close(); ReadLog(); }
        }
    }
    
    [GeneratedRegex("\\[Behaviour\\]\\s+(\\w+)\\s+(.+?)\\s+\\((usr_[a-f0-9-]+)\\)")]
    private partial Regex RegexLine();
    void ParseLine(string line)
    {
        var match = RegexLine().Match(line);
        if (!match.Success) return;
        switch (match.Groups[1].Value)
        {
            case "OnPlayerJoined":
                AddPlayer(match.Groups[2].Value, match.Groups[3].Value);
                break;
            case "OnPlayerLeft":
                RemovePlayer(match.Groups[3].Value);
                break;
        }
    }

    void AddPlayer(string name, string id)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            players.Add((name, id));
            userPanel.Children.Add(new TextBlock { Name = id, Text = name });
            Console.WriteLine(userPanel.Children.Count);
            //Console.WriteLine($"Added player {name} with id {id}");
            
        });
    }

    void RemovePlayer(string id)
    {
        Dispatcher.UIThread.Invoke(() =>
        {
            players.RemoveAll(p => p.id == id);
            for (int i = 0; i < userPanel.Children.Count; i++)
            {
                if ( userPanel.Children[i].Name == id)
                {
                    userPanel.Children.RemoveAt(i);
                }
            }
        });
    }

}