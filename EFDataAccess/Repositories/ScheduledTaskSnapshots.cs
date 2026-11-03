using System;
using System.Globalization;
using System.Linq;
using System.Net;
using EFDataAccess.Models;
using MyFinanceBackend.Services;
using MyFinanceModel;
using MyFinanceModel.ViewModel;

namespace EFDataAccess.Repositories
{
    public static class ScheduledTaskSnapshots
    {
        public static BaseScheduledTaskVm Create(AutomaticTask task)
        {
            BaseScheduledTaskVm definition;
            if (task.SpInTrxDef != null && task.TransferTrxDef == null)
                definition = new BasicScheduledTaskVm { IsSpend = task.SpInTrxDef.IsSpendTrx };
            else if (task.TransferTrxDef != null && task.SpInTrxDef == null)
                definition = new TransferScheduledTaskVm { ToAccountId = task.TransferTrxDef.ToAccountId };
            else throw new ServiceException("The task must have exactly one transaction definition.", HttpStatusCode.Conflict);
            definition.Id = task.AutomaticTaskId;
            definition.AccountId = task.AccountId;
            definition.Amount = (float)task.Amount;
            definition.CurrencyId = task.CurrencyId;
            definition.SpendTypeId = task.SpendTypeId;
            definition.Description = task.TaskDescription;
            definition.IsPending = task.IsPending;
            definition.FrequencyType = (ScheduledTaskFrequencyType)task.PeriodTypeId;
            try
            {
                definition.Days = string.IsNullOrWhiteSpace(task.Days) ? Array.Empty<int>() :
                    task.Days.Split(',').Select(x => int.Parse(x, NumberStyles.Integer, CultureInfo.InvariantCulture)).Distinct().ToArray();
            }
            catch (Exception error) when (error is FormatException or OverflowException)
            { throw new ServiceException("The task contains invalid schedule days.", HttpStatusCode.BadRequest); }
            ScheduledExecutionRules.ValidateSchedule(definition.FrequencyType, definition.Days);
            return definition;
        }
    }
}
