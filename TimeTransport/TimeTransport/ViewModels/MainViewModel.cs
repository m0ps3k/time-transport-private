using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimeTransport.Models;
using TimeTransport.Services;

namespace TimeTransport.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    public List<string> stops = new()
    {
        "М. Ломоносовская", "Володарский мост", "Октябрьская наб. 106", "М. Улица Дыбенко", "Товарищеский проспект"
    };

    [ObservableProperty]
    public bool directionIsBackward = false;

    [ObservableProperty]
    public string selectedStop = "М. Ломоносовская";

    public TimetableService timetableService;

    public Timetable timetable;

    public List<List<DisplayType>> AllTimetable;

    [ObservableProperty]
    public ObservableCollection<DisplayType> timetableBefore = new();

    [ObservableProperty]
    public ObservableCollection<DisplayType> timetableNow = new();

    [ObservableProperty]
    public ObservableCollection<DisplayType> timetableAfter = new();


    public MainViewModel(TimetableService _timetableService)
    {
        timetableService = _timetableService;
        if (DateTime.Now.Hour <= 12 && DateTime.Now.Hour > 2) directionIsBackward = false;
        else directionIsBackward = true;
        if (DateTime.Now.Hour <= 20 && DateTime.Now.Hour > 2) SelectedStop = "М. Ломоносовская";
        else SelectedStop = "Володарский мост";
        _ = LoadAsync();
    }

    public async Task LoadAsync()
    {
        timetable = await timetableService.LoadAsync();
        await ChangeTimetable();
    }

    [RelayCommand]
    public async Task ChangeToForward()
    {
        DirectionIsBackward = false;
        await ChangeTimetable();
    }


    [RelayCommand]
    public async Task ChangeToBackward()
    {
        DirectionIsBackward = true;
        await ChangeTimetable();
    }

    public async Task ChangeTimetable()
    {
        AllTimetable = await timetableService.GetCurrentTimetableAsync(timetable, DirectionIsBackward ? SelectedStop : "Уткина Заводь", DirectionIsBackward);
        TimetableBefore = new ObservableCollection<DisplayType>(AllTimetable[0]);
        TimetableNow = new ObservableCollection<DisplayType>(AllTimetable[1]);
        TimetableAfter = new ObservableCollection<DisplayType>(AllTimetable[2]);
    }

    partial void OnSelectedStopChanged(string value)
    {
        _ = ChangeTimetable();
    }
}
