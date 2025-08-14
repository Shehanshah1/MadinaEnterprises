using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using MadinaEnterprises.Modules.Models;

namespace MadinaEnterprises.Modules.Util
{
    public class NotificationService
    {
        private static NotificationService? _instance;
        private readonly DatabaseService _db;
        private System.Timers.Timer? _notificationTimer;
        private readonly List<Notification> _notifications = new();

        public static NotificationService Instance
        {
            get
            {
                _instance ??= new NotificationService();
                return _instance;
            }
        }

        private NotificationService()
        {
            _db = App.DatabaseService!;
            StartNotificationMonitoring();
        }

        private void StartNotificationMonitoring()
        {
            // Check for notifications every 5 minutes
            _notificationTimer = new System.Timers.Timer(300000); // 5 minutes
            _notificationTimer.Elapsed += async (s, e) => await CheckForNotifications();
            _notificationTimer.Start();

            // Initial check
            Task.Run(async () => await CheckForNotifications());
        }

        private async Task CheckForNotifications()
        {
            try
            {
                var contracts = await _db.GetAllContracts();
                var payments = await _db.GetAllPayments();
                var deliveries = await _db.GetAllDeliveries();

                _notifications.Clear();

                // Check for overdue payments (30 days)
                foreach (var contract in contracts)
                {
                    var contractPayments = payments.Where(p => p.ContractID == contract.ContractID).Sum(p => p.AmountPaid);
                    var daysOld = (DateTime.Now - contract.DateCreated).TotalDays;

                    if (contractPayments < contract.TotalAmount && daysOld > 30)
                    {
                        _notifications.Add(new Notification
                        {
                            Id = Guid.NewGuid().ToString(),
                            Type = NotificationType.OverduePayment,
                            Title = "Overdue Payment",
                            Message = $"Contract {contract.ContractID} has ${contract.TotalAmount - contractPayments:N2} overdue for {daysOld:F0} days",
                            Severity = NotificationSeverity.High,
                            ContractId = contract.ContractID,
                            CreatedAt = DateTime.Now
                        });
                    }

                    // Check for pending deliveries
                    var contractDeliveries = deliveries.Where(d => d.ContractID == contract.ContractID).Sum(d => d.TotalBales);
                    if (contractDeliveries < contract.TotalBales)
                    {
                        var pendingBales = contract.TotalBales - contractDeliveries;
                        var percentDelivered = (double)contractDeliveries / contract.TotalBales * 100;

                        if (percentDelivered < 50 && daysOld > 15)
                        {
                            _notifications.Add(new Notification
                            {
                                Id = Guid.NewGuid().ToString(),
                                Type = NotificationType.PendingDelivery,
                                Title = "Delivery Alert",
                                Message = $"Contract {contract.ContractID} has {pendingBales} bales pending delivery ({percentDelivered:F1}% completed)",
                                Severity = NotificationSeverity.Medium,
                                ContractId = contract.ContractID,
                                CreatedAt = DateTime.Now
                            });
                        }
                    }

                    // Check for contracts nearing completion
                    if (contractPayments >= contract.TotalAmount * 0.9 && contractPayments < contract.TotalAmount)
                    {
                        _notifications.Add(new Notification
                        {
                            Id = Guid.NewGuid().ToString(),
                            Type = NotificationType.ContractNearCompletion,
                            Title = "Contract Near Completion",
                            Message = $"Contract {contract.ContractID} is {(contractPayments / contract.TotalAmount * 100):F1}% complete",
                            Severity = NotificationSeverity.Low,
                            ContractId = contract.ContractID,
                            CreatedAt = DateTime.Now
                        });
                    }
                }

                // Check for low activity ginners (no contracts in 60 days)
                var ginners = await _db.GetAllGinners();
                foreach (var ginner in ginners)
                {
                    var recentContracts = contracts.Where(c =>
                        c.GinnerID == ginner.GinnerID &&
                        (DateTime.Now - c.DateCreated).TotalDays < 60).ToList();

                    if (!recentContracts.Any())
                    {
                        var lastContract = contracts
                            .Where(c => c.GinnerID == ginner.GinnerID)
                            .OrderByDescending(c => c.DateCreated)
                            .FirstOrDefault();

                        if (lastContract != null)
                        {
                            _notifications.Add(new Notification
                            {
                                Id = Guid.NewGuid().ToString(),
                                Type = NotificationType.InactiveGinner,
                                Title = "Inactive Ginner",
                                Message = $"{ginner.GinnerName} has no contracts in the last 60 days",
                                Severity = NotificationSeverity.Low,
                                CreatedAt = DateTime.Now
                            });
                        }
                    }
                }

                // Trigger UI update if notifications exist
                if (_notifications.Any(n => n.Severity == NotificationSeverity.High))
                {
                    await TriggerHighPriorityAlert();
                }
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"Notification check error: {ex.Message}");
            }
        }

        public async Task<List<Notification>> GetActiveNotifications()
        {
            await CheckForNotifications();
            return _notifications.OrderByDescending(n => n.Severity).ThenByDescending(n => n.CreatedAt).ToList();
        }

        public List<Notification> GetNotificationsByType(NotificationType type)
        {
            return _notifications.Where(n => n.Type == type).ToList();
        }

        public List<Notification> GetHighPriorityNotifications()
        {
            return _notifications.Where(n => n.Severity == NotificationSeverity.High).ToList();
        }

        public void DismissNotification(string notificationId)
        {
            _notifications.RemoveAll(n => n.Id == notificationId);
        }

        public void DismissAllNotifications()
        {
            _notifications.Clear();
        }

        private async Task TriggerHighPriorityAlert()
        {
            // In production, this would trigger push notifications or email alerts
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                var highPriorityCount = _notifications.Count(n => n.Severity == NotificationSeverity.High);
                if (highPriorityCount > 0 && Application.Current?.MainPage != null)
                {
                    await Application.Current.MainPage.DisplayAlert(
                        "Important Notifications",
                        $"You have {highPriorityCount} high priority notifications requiring attention.",
                        "View");
                }
            });
        }

        public void StopMonitoring()
        {
            _notificationTimer?.Stop();
            _notificationTimer?.Dispose();
        }
    }

    public class Notification
    {
        public string Id { get; set; } = "";
        public NotificationType Type { get; set; }
        public string Title { get; set; } = "";
        public string Message { get; set; } = "";
        public NotificationSeverity Severity { get; set; }
        public string? ContractId { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
    }

    public enum NotificationType
    {
        OverduePayment,
        PendingDelivery,
        ContractNearCompletion,
        InactiveGinner,
        LowInventory,
        SystemAlert
    }

    public enum NotificationSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }
}