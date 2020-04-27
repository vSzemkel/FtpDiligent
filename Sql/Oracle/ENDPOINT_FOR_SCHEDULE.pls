create or replace PROCEDURE              "ENDPOINT_FOR_SCHEDULE" (
   vsch_xx in ftp_schedule.xx%type,
   vCur out fgw.refCur
) as 
   vrefresh_date date;
begin
   if vsch_xx > 0 then
      open vCur for
         select fe.host,fe.userid,fe.passwd,fe.remote_dir,fe.local_dir,fe.refresh_date,sysdate,fe.direction,fe.transfer_mode
           from ftp_endpoint fe,ftp_schedule fs
          where fs.xx=vsch_xx and fs.end_xx=fe.xx;

      --zablokuj ponawianie tego zlecenia
      select get_next_sync(vsch_xx)+(select job_step-1 from ftp_schedule where xx=vsch_xx)/fgw.cMinutesPerDay
        into vrefresh_date from dual;
      update ftp_endpoint set refresh_date=nvl(vrefresh_date,refresh_date)
       where xx in (select end_xx from ftp_schedule where xx=vsch_xx);
      commit;
   else
      open vCur for
         select fe.host,fe.userid,fe.passwd,fe.remote_dir,fe.local_dir,fe.refresh_date,sysdate,fe.direction,fe.transfer_mode
           from ftp_endpoint fe
          where fe.xx=-vsch_xx;
   end if;
end endpoint_for_schedule;