create or replace PACKAGE "FGW" is
  vfShortDate varchar2(10) := 'dd/mm/rrrr';
  vfLongDate varchar2(18) := 'dd/mm/rrrr hh24:mi';

  cMinimumDelay   constant binary_integer := 5 ;
  cMinutesPerDay  constant binary_integer := 1440 ;

  type refCur is ref cursor;
  type str256list is table of varchar2(256) index by binary_integer;
  type numlist is table of number index by binary_integer;
  type datelist is table of date index by binary_integer;
end;