﻿using ShareBook.Repository;
using ShareBook.Service;
using ShareBook.Domain.Enums;
using System;
using ShareBook.Domain;
using ShareBook.Domain.Exceptions;
using Microsoft.Extensions.Configuration;

namespace Sharebook.Jobs
{
    public class MeetupSearch : GenericJob, IJob
    {
        private readonly IMeetupService _meetupService;
        private readonly IConfiguration _configuration;
        public MeetupSearch(IJobHistoryRepository jobHistoryRepo, IMeetupService meetupService, IConfiguration configuration) : base(jobHistoryRepo)
        {
            _meetupService = meetupService;

            JobName = "MeetupSearch";
            Description = "Mantém uma lista atualizada de eventos do sharebook no sympla juntamente com os links para a live no youtube";
            Interval = Interval.Dayly;
            Active = true;
            BestTimeToExecute = new TimeSpan(0, 0, 0);
            _configuration = configuration;
        }

        public override JobHistory Work()
        {
            var meetupEnabled = bool.Parse(_configuration["MeetupSettings:IsActive"]);
            if(!meetupEnabled) throw new MeetupDisabledException("Serviço aws sqs está desabilitado no appsettings.");
            
            var jobResult = _meetupService.FetchMeetups().Result;

            return new JobHistory()
            {
                JobName = JobName,
                IsSuccess = true,
                Details = jobResult
            };
        }
    }
}
