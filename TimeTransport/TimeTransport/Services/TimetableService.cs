using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TimeTransport.Models;


namespace TimeTransport.Services;

public class TimetableService
{
    public async Task<Timetable> LoadAsync()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(
            "TimeTransport.Data.timetable.json");
        var data = await JsonSerializer.DeserializeAsync<Timetable>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        return data;
    }

    public async Task<List<List<DisplayType>>> GetCurrentTimetableAsync(Timetable timetable, string stop, bool DirectionIsBackward)
    {
        Dictionary<string, string> stopsDic = new Dictionary<string, string>()
        {
            ["Уткина Заводь"] = "utkina_zavod",
            ["М. Ломоносовская"] = "lomono",
            ["Володарский мост"] = "volodarsky_bridge",
            ["Октябрьская наб. 106"] = "october_emb_106",
            ["Товарищеский проспект"] = "tov_prospect",
            ["М. Улица Дыбенко"] = "dybenko_metro"
        };

        string stopId = stopsDic[stop];
        DateTime dateTimeNow = DateTime.Now;
        Dictionary<string, Dictionary<string, List<string>>> stopSchedule = new(); //Номер маршрута, Словарь: Часы: Лист из минут
        DayOfWeek logical = dateTimeNow.AddHours(-3).DayOfWeek;
        bool isWeekday = logical != DayOfWeek.Saturday && logical != DayOfWeek.Sunday;


        if (!DirectionIsBackward)
        {
            foreach (var route in timetable.Routes)
            {
                var stops = route.Directions.Forward.Stops;
                foreach (var stopItem in stops)
                {
                    if (stopItem.Id == stopId)
                    {
                        if (isWeekday)
                        {
                            stopSchedule.Add(route.Number, stopItem.Schedule.Weekday);
                        }
                        else
                        {
                            stopSchedule.Add(route.Number, stopItem.Schedule.Weekend);
                        }
                    }
                }
            }
        }
        else
        {
            foreach (var route in timetable.Routes)
            {
                var stops = route.Directions.Backward.Stops;
                foreach (var stopItem in stops)
                {
                    if (stopItem.Id == stopId)
                    {
                        if (isWeekday)
                        {
                            stopSchedule.Add(route.Number, stopItem.Schedule.Weekday);
                        }
                        else
                        {
                            stopSchedule.Add(route.Number, stopItem.Schedule.Weekend);
                        }
                    }
                }
            }
        }

        

        List<string> Routes = stopSchedule.Keys.ToList();
        int CurrentHour = DateTime.Now.Hour;
        List<FilterSchedule> filterSchedules = new();
        foreach (var route in Routes)
        {
            List<string> MinutesBefore = new();
            List<string> MinutesCurrent = new();
            List<string> MinutesAfter = new();

            if (stopSchedule.TryGetValue(route, out var routeScheduleBefore) && routeScheduleBefore.TryGetValue(Convert.ToString((CurrentHour - 1 != -1) ? (CurrentHour - 1) : 23), out var departuresBefore))
                MinutesBefore = departuresBefore;

            if (stopSchedule.TryGetValue(route, out var routeSchedule) && routeSchedule.TryGetValue(Convert.ToString(CurrentHour), out var departures))
                MinutesCurrent = departures;

            if (stopSchedule.TryGetValue(route, out var routeScheduleAfter) && routeScheduleAfter.TryGetValue(Convert.ToString((CurrentHour + 1 != 24) ? (CurrentHour + 1) : 0), out var departuresAfter))
                MinutesAfter = departuresAfter;

            foreach (var i in MinutesBefore)
            {
                var departure = TimeOnly.ParseExact(i, "HH:mm", System.Globalization.CultureInfo.InvariantCulture);
                filterSchedules.Add(new FilterSchedule { Hour = departure.Hour, Minute = departure.Minute, Route = route });
            }
            foreach (var i in MinutesCurrent)
            {
                var departure = TimeOnly.ParseExact(i, "HH:mm", System.Globalization.CultureInfo.InvariantCulture);
                filterSchedules.Add(new FilterSchedule { Hour = departure.Hour, Minute = departure.Minute, Route = route });
            }
            foreach (var i in MinutesAfter)
            {
                var departure = TimeOnly.ParseExact(i, "HH:mm", System.Globalization.CultureInfo.InvariantCulture);
                filterSchedules.Add(new FilterSchedule { Hour = departure.Hour, Minute = departure.Minute, Route = route });
            }
        }

        var TimeNow = TimeOnly.FromDateTime(DateTime.Now);
        List<DisplayType> finalScheduleBefore = new();
        List<DisplayType> finalScheduleNow = new();
        List<DisplayType> finalScheduleAfter = new();

        Dictionary<string, string> dest;
        if (DirectionIsBackward)
            dest = new() { ["119"] = "Уткина заводь", ["285"] = "Уткина заводь", ["4"] = "Уткина заводь", ["8"] = "Уткина заводь, старое кольцо", ["476"] = "Пос. Им. Свердлова" };
        else
            dest = new() { ["119"] = "М. Ломоносовская", ["285"] = "Р. Оккервиль", ["4"] = "Р. Оккервиль" };

        foreach (var item in filterSchedules)
        {
            string CurrentDest = dest[item.Route];

            TimeOnly time = new TimeOnly(item.Hour, item.Minute);
            var diff = TimeNow.ToTimeSpan() - time.ToTimeSpan();

            if (diff > TimeSpan.FromHours(12)) diff -= TimeSpan.FromDays(1);
            else if (diff < -TimeSpan.FromHours(12)) diff += TimeSpan.FromDays(1);

            if (diff.TotalMinutes > 1 && diff.TotalMinutes <= 6) finalScheduleBefore.Add(new() { Number = item.Route, Destination = CurrentDest, Time = Convert.ToInt32(Math.Round(diff.TotalMinutes)) });
            else if (diff.TotalMinutes > 0 && diff.TotalMinutes <= 1) finalScheduleNow.Add(new() { Number = item.Route, Destination = CurrentDest, Time = 0 });
            else if (diff.TotalMinutes < 0 && diff.TotalMinutes >= -30) finalScheduleAfter.Add(new() { Number = item.Route, Destination = CurrentDest, Time = Convert.ToInt32(Math.Round(-diff.TotalMinutes)) });
        }

        List<List<DisplayType>> finalSchedule = new();
        finalSchedule.Add(finalScheduleBefore.OrderByDescending(p => p.Time).ToList());
        finalSchedule.Add(finalScheduleNow);
        finalSchedule.Add(finalScheduleAfter.OrderBy(p => p.Time).ToList());

        return finalSchedule;
    }
}
