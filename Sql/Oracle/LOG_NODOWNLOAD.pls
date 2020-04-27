create or replace PROCEDURE              "LOG_NODOWNLOAD" (
   vsch_xx in ftp_schedule.xx%type,
   vrefresh_date in date
) as begin
   if vsch_xx > 0 then
      update ftp_endpoint set refresh_date=vrefresh_date
       where xx in (select end_xx from ftp_schedule where xx=vsch_xx);
   else
      update ftp_endpoint set refresh_date=vrefresh_date where xx=-vsch_xx;
   end if;
end log_nodownload;