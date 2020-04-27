create or replace PROCEDURE init_instance (
  vnazwa in ftp_worker_instance.nazwa%type,
  vxx out ftp_worker_instance.xx%type
) is begin
   select min(xx) into vxx from ftp_worker_instance where nazwa=vnazwa;
   if vxx is null then
      insert into ftp_worker_instance (nazwa)
           values (vnazwa) returning xx into vxx;
   end if;
end init_instance;