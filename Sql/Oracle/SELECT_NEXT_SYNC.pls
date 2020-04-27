create or replace PROCEDURE "SELECT_NEXT_SYNC" (
   vins_xx in ftp_worker_instance.xx%type,
   vCur out fgw.refCur
) as 
   vsch_xx ftp_schedule.xx%type;
   vsync_date date;
begin
   select xx,sync_date into vsch_xx,vsync_date from
     (select rownum rn,t.xx,t.sync_date
        from (select fs.xx,get_next_sync(fs.xx) sync_date
                from ftp_endpoint fe,ftp_schedule fs
               where fe.ins_xx=vins_xx and fe.xx=fs.end_xx
                 and fs.disabled is null and fs.usuniety is null and fe.usuniety is null
               order by get_next_sync(fs.xx),fe.refresh_date,fs.job_step) t) t2
   where t2.rn=1;
   
   if vsync_date is null or get_week_start + 7 < vsync_date then
      vsync_date := get_week_start + 7;
      vsch_xx := -1;
   end if;
   
   open vCur for
      select xx,nazwa,vsync_date from ftp_schedule fs where xx=vsch_xx
      union all select -1,'next week',vsync_date from dual where vsch_xx=-1;
end select_next_sync;