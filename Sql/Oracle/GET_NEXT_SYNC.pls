create or replace FUNCTION              "GET_NEXT_SYNC" (vsch_xx in ftp_schedule.xx%type) return date is
   vret date;
begin
   select min(get_week_start+(job_start/fgw.cMinutesPerDay)+p.x*job_step/fgw.cMinutesPerDay) sync_time
     into vret
     from ftp_schedule fs, ftp_endpoint fe, pivot p
    where fs.xx=vsch_xx and fs.end_xx=fe.xx and p.x <= (fs.job_stop-fs.job_start)/fs.job_step
      and fe.refresh_date<get_week_start+(job_start/fgw.cMinutesPerDay)+p.x*job_step/fgw.cMinutesPerDay
      and sysdate<=get_week_start+(job_start/fgw.cMinutesPerDay)+(p.x*job_step+5)/fgw.cMinutesPerDay; --5 minut po czasie moga byc startowane kolejne prace o nakladajacym sie poczatku, stad "+5"
     
   return vret;
end get_next_sync;