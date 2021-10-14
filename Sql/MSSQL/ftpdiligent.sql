
-- -----------------------------------------------------------------------
-- <copyright file="ftpdiligent.sql" company="Agora SA">
-- <legal>Copyright (c) Development IT, kwiecien-maj 2020</legal>
-- <author>Marcin Buchwald</author>
-- </copyright>
-- -----------------------------------------------------------------------


drop table if exists [ftp].[ftp_file]
go
drop table if exists [ftp].[ftp_schedule]
go
drop table if exists [ftp].[ftp_endpoint]
go
drop table if exists [ftp].[ftp_instance]
go

create table [ftp].[ftp_instance] (
    xx int identity not null,
    hostname varchar(128) not null,
    timezone varchar(128) not null,
    disposition varchar(128) default 'do ustalenia' not null,
    administrator varchar(64) default 'do ustalenia' not null,
    email2adm varchar(64) default 'do ustalenia' not null,
    created datetime2 constraint df_instance_creation default getdate() not null,
    disabled bit null,
    constraint pk_instance primary key clustered (xx),
)
go

create table [ftp].[ftp_endpoint] (
    xx int identity not null,
    ins_xx int not null,
    host varchar(64) not null,
    userid varchar(32) not null,
    passwd varchar(32) not null,
    local_dir varchar(256) not null,
    remote_dir varchar(256) not null,
    refresh_date datetime2 constraint df_endpoint_refresh default getdate() not null,
    protocol tinyint not null,
    direction tinyint not null,
    transfer_mode tinyint not null,
    disabled bit,
    constraint pk_endpoint primary key clustered (xx),
    constraint fk_endpoint_instance foreign key (ins_xx) references [ftp].[ftp_instance] (xx)
)
go

create table [ftp].[ftp_schedule] (
    xx int identity not null,
    end_xx int not null,
    name varchar(128) not null,
    job_start smallint not null,
    job_stop smallint not null,
    job_stride smallint not null,
    deleted bit,
    disabled bit,
    constraint pk_schedule primary key clustered (xx),
    constraint fk_schedule_endpoint foreign key (end_xx) references [ftp].[ftp_endpoint] (xx)
)
go

create table [ftp].[ftp_file] (
    xx int identity not null,
    end_xx int null,
    sch_xx int null,
    file_name varchar(128) not null,
    file_size bigint not null,
    file_date datetime2 not null,
    md5 binary(16) not null,
    completion_date datetime2 constraint df_file_completion default getdate(),
    direction tinyint not null,
    transfer_mode tinyint not null,
    manual_sync bit,
    disabled bit,
    constraint pk_file primary key clustered (xx),
    constraint fk_file_endpoint foreign key (end_xx) references [ftp].[ftp_endpoint] (xx),
    constraint fk_file_schedule foreign key (sch_xx) references [ftp].[ftp_schedule] (xx)
)
go



drop procedure [ftp].[sp_init_instance]
go
create procedure [ftp].[sp_init_instance] (
    @hostname varchar(128),
    @zone varchar(128)
) as begin
    declare @xx int
    select @xx=xx from [ftp].[ftp_instance] where hostname=@hostname
    if @xx is null
        insert into [ftp].[ftp_instance] (hostname,timezone)
        output inserted.xx values (@hostname,@zone)
    else
        select @xx
end
go

drop procedure [ftp].[sp_modify_endpoint]
go
create procedure [ftp].[sp_modify_endpoint] (
    @mode tinyint,
    @xx int,
    @ins_xx int,
    @host varchar(64),
    @userid varchar(32),
    @passwd varchar(32),
    @remdir varchar(256),
    @locdir varchar(256),
    @protocol tinyint,
    @direction tinyint,
    @transfer_mode tinyint
) as begin
  if @mode=0
     insert into [ftp].[ftp_endpoint] (ins_xx,host,userid,passwd,remote_dir,local_dir,protocol,direction,transfer_mode)
     output inserted.xx
     values (@ins_xx,@host,@userid,@passwd,@remdir,@locdir,@protocol,@direction,@transfer_mode)
  else if @mode=1
    update [ftp].[ftp_endpoint]
    set host=@host,userid=@userid,passwd=@passwd,remote_dir=@remdir,
        local_dir=@locdir,protocol=@protocol,direction=@direction,transfer_mode=@transfer_mode
    where xx=@xx
  else begin
     delete from [ftp].[ftp_file] where end_xx=@xx or sch_xx in (select xx from [ftp].[ftp_schedule] where end_xx=@xx and deleted=1)
     delete from [ftp].[ftp_schedule] where end_xx=@xx and deleted=1
     delete from [ftp].[ftp_endpoint] where xx=@xx
  end
end
go

drop procedure [ftp].[sp_modify_schedule]
go
create procedure [ftp].[sp_modify_schedule] (
    @mode tinyint,
    @xx int,
    @end_xx int,
    @name varchar(128),
    @job_start smallint,
    @job_stop smallint,
    @job_stride smallint,
    @disabled bit
) as begin
  if @job_stop < @job_start
    raiserror(N'Data zakonczenia musi byc pozniejsza niz data rozpoczecia', 16, 1);
  
  if @mode=0
     insert into [ftp].[ftp_schedule] (end_xx,name,job_start,job_stop,job_stride,disabled)
     output inserted.xx
     values (@end_xx,@name,@job_start,@job_stop,@job_stride,case @disabled when 0 then null else 1 end)
  else if @mode=1
    update [ftp].[ftp_schedule] set name=@name,disabled=case @disabled when 0 then null else 1 end,
           job_start=@job_start,job_stop=@job_stop,job_stride=@job_stride
     where xx=@xx;
  else
     update [ftp].[ftp_schedule] set deleted=1 where xx=@xx;
end
go

drop procedure [ftp].[sp_check_file]
go
create procedure [ftp].[sp_check_file] (
    @ins_xx int,
    @file_name varchar(128),
    @file_size bigint,
    @file_date datetime2
) as begin
    declare @cnt int
    select @cnt=count(1) from [ftp].[ftp_file] ff,[ftp].[ftp_endpoint] fe left outer join [ftp].[ftp_schedule] fs on fe.xx=fs.end_xx
     where fe.ins_xx=@ins_xx
       and ((fs.xx=ff.sch_xx and ff.manual_sync is null) or (fe.xx=ff.end_xx and ff.manual_sync is not null))
       and ff.file_date=@file_date and ff.file_name=@file_name and ff.file_size=@file_size
    if @cnt > 0
        select 1
    select 0
end
go

drop procedure [ftp].[sp_endpoint_for_schedule]
go
create procedure [ftp].[sp_endpoint_for_schedule] (
    @xx int
) as begin
    if @xx > 0
        select fe.host,fe.userid,fe.passwd,fe.remote_dir,fe.local_dir,fe.refresh_date,fe.protocol,fe.direction,fe.transfer_mode
          from [ftp].[ftp_endpoint] fe,ftp_schedule fs
         where fs.xx=@xx and fs.end_xx=fe.xx
    else
        select host,userid,passwd,remote_dir,local_dir,refresh_date,protocol,direction,transfer_mode
          from [ftp].[ftp_endpoint] where xx=-@xx
end
go

drop procedure [ftp].[sp_select_next_sync]
go
create procedure [ftp].[sp_select_next_sync] (
    @ins_xx int
) as begin
    select * from (select top 1 fs.xx,name,[ftp].[sf_get_next_sync](fs.xx) as sync
      from [ftp].[ftp_endpoint] fe,[ftp].[ftp_schedule] fs
     where fe.ins_xx=@ins_xx and fe.xx=fs.end_xx
       and fe.disabled is null and fs.disabled is null
       and [ftp].[sf_get_week_minute]() < fs.job_stop
     order by sync,refresh_date,job_stride) t
 union all
    select -1,'next week',dateadd(day,7,[ftp].[sf_get_week_start]()) as sync
end
go

drop procedure [ftp].[sp_log_activation]
go
create procedure [ftp].[sp_log_activation] (
    @xx int,
    @refresh_date datetime2
) as begin
   if @xx > 0
      update [ftp].[ftp_endpoint] set refresh_date=@refresh_date
       where xx in (select end_xx from [ftp].[ftp_schedule] where xx=@xx)
   else
      update [ftp].[ftp_endpoint] set refresh_date=@refresh_date where xx=-@xx
end
go

drop procedure [ftp].[sp_log_download]
go
create procedure [ftp].[sp_log_download] (
    @direction tinyint,
    @xx int,
    @refresh_date datetime2,
    @file_name varchar(128),
    @file_size bigint,
    @file_date datetime2,
    @md5 binary(16)
) as begin
    if @xx > 0 begin
        update [ftp].[ftp_endpoint] set refresh_date=@refresh_date
         where xx in (select end_xx from [ftp].[ftp_schedule] where xx=@xx)
        insert into [ftp].[ftp_file] (sch_xx,file_name,file_size,file_date,md5,manual_sync,direction,transfer_mode)
             select @xx,@file_name,@file_size,@file_date,@md5,null,@direction,transfer_mode
               from [ftp].[ftp_endpoint] fe,[ftp].[ftp_schedule] fs
              where fe.xx=fs.end_xx and fs.xx=@xx
    end else begin
        update [ftp].[ftp_endpoint] set refresh_date=@refresh_date where xx=-@xx
        insert into [ftp].[ftp_file] (end_xx,file_name,file_size,file_date,md5,manual_sync,direction,transfer_mode)
             select -@xx,@file_name,@file_size,@file_date,@md5,1,@direction,transfer_mode
               from [ftp].[ftp_endpoint] where xx=-@xx
    end
end
go

drop function [ftp].[sf_get_week_start]
go
create function [ftp].[sf_get_week_start]()
returns datetimeoffset
as begin
    return (select cast(cast(dateadd(day, 1 - datepart(dw, getdate()), getdate()) as date) as datetime)
   at time zone fi.timezone
      from [sys].[dm_exec_sessions] es, [ftp].[ftp_instance] fi
     where es.session_id=@@spid and es.host_name=fi.hostname collate database_default)
end
go

drop function [ftp].[sf_get_week_minute]
go
create function [ftp].[sf_get_week_minute]()
returns smallint
as begin
    return (select datediff(mi, [ftp].[sf_get_week_start](), getdate()))
end
go

-- Casual week contains 10080 minutes (60 * 24 * 7)
-- One can define weekly repeat sync series as (job_start, job_stop, job_stride)
-- This function returns next or first missed series occurence
-- CASE1: job_stop < week_minute - function not called for CASE1
-- CASE2: week_minute < job_start - function returns job_start as datetimeoffset
-- CASE3: missed executions - function returns first missed datetimeoffset
-- CASE4: scheduled execution lasted less then one minute, returns next after planned
-- CASE5: work in progress - function returns planned sync time
drop function [ftp].[sf_get_next_sync]
go
create function [ftp].[sf_get_next_sync] (
    @xx int
)
returns datetimeoffset
as begin
    declare @week_start datetime, @week_minute smallint

     select @week_start = [ftp].[sf_get_week_start](),
            @week_minute = [ftp].[sf_get_week_minute]()

    return (select case
        when @week_minute < job_start then aux.startlocal
        when aux.startlocal < aux.retlocal and aux.missed < aux.retlocal then aux.missed
        when aux.retlocal < aux.refresh then dateadd(minute, aux.job_stride, aux.retlocal)
        else aux.retlocal end
    from (select fs.job_start,fs.job_stride,
                refresh_date at time zone fi.timezone as refresh,
                dateadd(minute, job_start, @week_start) at time zone fi.timezone as startlocal,
                dateadd(minute, job_stride, refresh_date) at time zone fi.timezone as missed,
                (select dateadd(minute, job_start + ceiling(cast(@week_minute - job_start as float) / job_stride) * job_stride, @week_start)) at time zone fi.timezone as retlocal
            from [ftp].[ftp_endpoint] fe, [ftp].[ftp_schedule] fs, [ftp].[ftp_instance] fi, [sys].[dm_exec_sessions] es
           where fe.xx = fs.end_xx and fs.xx = @xx and es.session_id = @@spid and es.host_name=fi.hostname collate database_default) aux)
end
go

---------------- FOR (LocalDB)\MSSQLLocalDB -------------------------

drop function [ftp].[sf_get_week_start_local]
go
create function [ftp].[sf_get_week_start_local]()
returns datetimeoffset
as begin
    return (select cast(cast(dateadd(day, 1 - datepart(dw, getdate()), getdate()) as date) as datetime))
end
go

drop function [ftp].[sf_get_week_minute_local]
go
create function [ftp].[sf_get_week_minute_local]()
returns smallint
as begin
    return (select datediff(mi, [ftp].[sf_get_week_start_local](), getdate()))
end
go

drop function [ftp].[sf_get_next_sync_local]
go
create function [ftp].[sf_get_next_sync_local] (
    @xx int
)
returns datetimeoffset
as begin
    declare @week_start datetime, @week_minute smallint

     select @week_start = [ftp].[sf_get_week_start_local](),
            @week_minute = [ftp].[sf_get_week_minute_local]()

    return (select case
        when @week_minute < job_start then aux.startlocal
        when aux.startlocal < aux.retlocal and aux.missed < aux.retlocal then aux.missed
        when aux.retlocal < aux.refresh_date then dateadd(minute, aux.job_stride, aux.retlocal)
        else aux.retlocal end
    from (select fs.job_start,fs.job_stride,fe.refresh_date,
                 dateadd(minute, job_start, @week_start) as startlocal,
                 dateadd(minute, job_stride, refresh_date) as missed,
                (select dateadd(minute, job_start + ceiling(cast(@week_minute - job_start as float) / job_stride) * job_stride, @week_start)) as retlocal
            from [ftp].[ftp_endpoint] fe, [ftp].[ftp_schedule] fs, [ftp].[ftp_instance] fi, [sys].[dm_exec_sessions] es
           where fe.xx = fs.end_xx and fs.xx = @xx and es.session_id = @@spid and es.host_name=fi.hostname collate database_default) aux)
end
go
