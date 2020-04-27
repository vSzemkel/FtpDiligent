create or replace function get_week_start return date
as
   vret date := sysdate + fgw.cMinimumDelay/fgw.cMinutesPerDay; -- przesuniecie o minimalna przerwe miedzy transferami do obslugi nocy sb/nd
begin
   select trunc(vret) - to_number(to_char(vret,'d')) into vret from dual;
   return vret;
end get_week_start;
