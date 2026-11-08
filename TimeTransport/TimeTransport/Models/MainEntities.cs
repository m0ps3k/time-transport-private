using System;
using System.Collections.Generic;
using Avalonia.Collections;
using CommunityToolkit.Mvvm.DependencyInjection;

namespace TimeTransport.Models;

public class Schedule
{
    public Dictionary<string, List<string>> Weekday { get; set; } = [];
    public Dictionary<string, List<string>> Weekend { get; set; } = [];
}

public class Stop
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public Schedule Schedule { get; set; } = new();
}

public class Direction
{
    public List<Stop> Stops { get; set; } = [];
}

public class Directions
{
    public Direction Forward { get; set; } = new();
    public Direction Backward { get; set; } = new();
}

public class Route
{
    public string Number { get; set; } = "";
    public Directions Directions { get; set; } = new();
}

public class Timetable
{
    public List<Route> Routes { get; set; } = [];
}

public record DisplayType
{
    public string Number { get; set; } = "";
    public string Destination { get; set; } = "";
    public int Time { get; set; }
}

public class FilterSchedule
{
    public int Hour { get; set; } = 0;
    public int Minute { get; set; } = 0;
    public string Route { get; set; } = "";
}