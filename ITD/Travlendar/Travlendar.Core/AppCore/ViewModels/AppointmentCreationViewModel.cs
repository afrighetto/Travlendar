﻿using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Travlendar.Core.AppCore.Model;
using Travlendar.Core.AppCore.Pages;
using Travlendar.Framework.ViewModels;
using Xamarin.Forms;

namespace Travlendar.Core.AppCore.ViewModels
{
    public class AppointmentCreationViewModel : BindableBaseNotify
    {
        private AppointmentCreationPage page;
        private INavigation navigation;
        private ObservableCollection<Appointment> appointments;
        private Appointment appointment;
        private string message;
        private string location;
        static public object[] settingsValue;
        private string titleAppointment;
        public string TitleAppointment
        {
            get { return titleAppointment; }
            set { this.SetProperty(ref titleAppointment, value); }
        }

        private bool isAllDayOn;
        public bool IsAllDayOn
        {
            get { return isAllDayOn; }
            set { SetProperty(ref isAllDayOn, value); }
        }

        private DateTime startDate;
        public DateTime StartDate
        {
            get { return startDate; }
            set { SetProperty(ref startDate, value); }
        }

        private TimeSpan startTime;
        public TimeSpan StartTime
        {
            get { return startTime; }
            set { SetProperty(ref startTime, value); }
        }

        private DateTime endDate;
        public DateTime EndDate
        {
            get { return endDate; }
            set { SetProperty(ref endDate, value); }
        }

        private TimeSpan endTime;
        public TimeSpan EndTime
        {
            get { return endTime; }
            set { SetProperty(ref endTime, value); }
        }

        private string detail;
        public string Detail
        {
            get { return detail; }
            set { SetProperty(ref detail, value); }
        }

        private Color color;
        public Color Color
        {
            get { return color; }
            set { SetProperty(ref color, value); }
        }

        private bool isAlertOn;
        public bool IsAlertOn
        {
            get { return isAlertOn; }
            set { SetProperty(ref isAlertOn, value); }
        }

        private Command saveAppointmentCommand;
        public ICommand SaveAppointmentCommand
        {
            get
            {
                return saveAppointmentCommand ?? (saveAppointmentCommand = new Command(async () =>
                {
                    if (string.IsNullOrWhiteSpace(this.TitleAppointment))
                    {
                        await page.DisplayAlert("Title cannot be empty.", "", "Ok");
                    }
                    else if (StartDate > EndDate || (StartDate < EndDate && StartTime > EndTime))
                    {
                        await page.DisplayAlert("Cannot Save Event", "The start date must be before the end date.", "Ok");
                    }
                    else
                    {
                        Appointment ap = new Appointment
                        {
                            Title = this.TitleAppointment,
                            IsAllDay = this.IsAllDayOn,
                            StartDate = this.StartDate.AddHours(StartTime.Hours).AddMinutes(StartTime.Minutes),
                            EndDate = this.EndDate.AddHours(EndTime.Hours).AddMinutes(EndTime.Minutes),
                            Detail = this.Detail,
                            Color = this.Color == Color.Default ? Color.FromRgb(28, 109, 107) : this.Color
                        };
                        await CreateAppointment(ap);
                    }
                }));
            }
        }

        private Command removeAppointmentCommand;
        public ICommand RemoveAppointmentCommand
        {
            get
            {
                return removeAppointmentCommand ?? (removeAppointmentCommand = new Command(async () =>
                {
                    appointments.Remove(appointment);
                    CognitoSyncViewModel.GetInstance().RemoveFromDataset("Appointment", appointment.GetHashCode().ToString());
                    await navigation.PopAsync(true);
                }));
            }
        }

        private Command navigateAppointmentCommand;
        public ICommand NavigateAppointmentCommand
        {
            get
            {
                SettingsViewModel settings = new SettingsViewModel(null, null);

                return navigateAppointmentCommand ?? (navigateAppointmentCommand = new Command(() =>
                {
                    NavigationViewModel.GetInstance().Navigate(location, settings.Car, settings.Bike, settings.PublicTransport, settings.MinimizeCarbonFootPrint);
                }));
            }
        }

        public AppointmentCreationViewModel(AppointmentCreationPage page, INavigation navigation, ObservableCollection<Appointment> aps, string msg, Appointment a, string location)
        {
            this.page = page;
            this.navigation = navigation;
            this.appointments = aps;
            this.message = msg;
            this.appointment = a;
            this.location = location;

            

            if (message == "Update")
            {
                this.TitleAppointment = a.Title;
                this.StartDate = a.StartDate;
                this.EndDate = a.EndDate;
                this.StartTime = a.StartDate.TimeOfDay;
                this.EndTime = a.EndDate.TimeOfDay;
                this.Detail = a.Detail;
                this.IsAllDayOn = a.IsAllDay;
                this.Color = a.Color;
            } else {
                this.StartDate = DateTime.Now;
                this.EndDate = DateTime.Now;
                this.StartTime = DateTime.Now.TimeOfDay;
                this.EndTime = DateTime.Now.TimeOfDay;
            }

            MessagingCenter.Subscribe<CalendarTypePage, Color>(this, "ColorEvent", (sender, color) =>
            {
                this.Color = color;
            });
        }

        private async Task CreateAppointment(Appointment newAppointment)
        {
            var overlappedEvent = appointments.FirstOrDefault(item => item.StartDate == newAppointment.StartDate || (item.StartDate <= newAppointment.EndDate && item.EndDate >= newAppointment.StartDate));
            if (overlappedEvent != null && message != "Update")
            {
                bool response = await page.DisplayAlert("Overlapped Event", String.Format("There's another event ({0}) scheduled for this time interval, are you sure to continue?", overlappedEvent.Title), "Continue", "Cancel");
                if (!response)
                {
                    return;
                }
            }

            if (System.Convert.ToBoolean(settingsValue[0])){

                TimeSpan timeBreak = (TimeSpan)settingsValue[1];
                TimeSpan timeInterval = (TimeSpan)settingsValue[2];
                if(newAppointment.StartDate.TimeOfDay == timeBreak || (timeBreak <= newAppointment.EndDate.TimeOfDay && timeBreak.Add(timeInterval) >= newAppointment.StartDate.TimeOfDay))
                {
                    if (!newAppointment.Title.ToLower().Contains("lunch"))
                    {
                        bool response = await page.DisplayAlert("Overlapped Event", "Lunch Break is active, are you sure to continue?", "Continue", "Cancel");
                        if (!response)
                        {
                            return;
                        }
                    }
                }
            }

            object[] values = new object[] { message, appointment, newAppointment };
            MessagingCenter.Send<AppointmentCreationPage, object[]>(this.page, "CreationAppointments", values);
            await navigation.PopAsync(true);
        }
    }
}