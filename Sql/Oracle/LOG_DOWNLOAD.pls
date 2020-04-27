create or replace PROCEDURE              "LOG_DOWNLOAD" (
   vdirection in binary_integer,
   vsch_xx in ftp_schedule.xx%type,
   vrefresh_date in date,
   vafile_names in fgw.str256list,
   vafile_sizes in fgw.numlist,
   vafile_dates in fgw.datelist
) as
   vtransfer_mode ftp_endpoint.transfer_mode%type;
begin
   if vsch_xx > 0 then
      update ftp_endpoint set refresh_date=vrefresh_date
       where xx in (select end_xx from ftp_schedule where xx=vsch_xx)
   returning transfer_mode into vtransfer_mode;
    
      forall i in vafile_names.first..vafile_names.last
         insert into ftp_file (sch_xx,file_name,file_size,file_date,manual_sync,direction,transfer_mode)
              values (vsch_xx,vafile_names(i),vafile_sizes(i),vafile_dates(i),null,vdirection,vtransfer_mode);
   else
      update ftp_endpoint set refresh_date=vrefresh_date 
       where xx=-vsch_xx returning transfer_mode into vtransfer_mode;
    
      forall i in vafile_names.first..vafile_names.last
         insert into ftp_file (end_xx,file_name,file_size,file_date,manual_sync,direction,transfer_mode)
              values (-vsch_xx,vafile_names(i),vafile_sizes(i),vafile_dates(i),1,vdirection,vtransfer_mode);
   end if;
end log_download;