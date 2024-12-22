select 
	EPG_EVENT.title as Title, 
	EPG_EVENT.genres as Genres,
	CHANNEL.name as ChannelName, 
	CHANNEL.number as ChannelMajor, 
	CHANNEL.minor as ChannelMinor, 
	datetime(min(start_time), 'localtime') as StartTime,
	--timediff(end_time, start_time)),
	ROUND((JULIANDAY(end_time) - JULIANDAY(start_time)) * 24, 1) AS Duration

from EPG_EVENT
join CHANNEL on CHANNEL.oid = EPG_EVENT.channel_oid

where (
	start_time > datetime('now') or end_time > datetime('now')
) 

and start_time < datetime('now', 'localtime','24 hours')

and EPG_EVENT.title not in (
	select distinct name from recurring_recording
)

and CHANNEL.name not in ('example1', 'example2')

and EPG_EVENT.genres not in (
	'example3',
	'example4',
	'example5',
)

and EPG_EVENT.genres not like '%Consumer%'

and EPG_EVENT.title not in (
	'Paid Program',
	'Paid Programming'
) COLLATE NOCASE

group by EPG_EVENT.title, CHANNEL.name, CHANNEL.number, CHANNEL.minor
order by start_time
