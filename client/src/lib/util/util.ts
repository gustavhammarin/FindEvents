import { format, formatDistanceToNow, type DateArg } from "date-fns";

export function formatDate(date: DateArg<Date>) {
    return format(date, 'dd MMM yyyy h:mm a');
}

export function timeAgo(date: DateArg<Date>) {
    return formatDistanceToNow(date) + ' ago';
}
