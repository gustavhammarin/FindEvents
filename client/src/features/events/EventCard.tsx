import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { Calendar, Clock, MapPin, ExternalLink } from "lucide-react";

type Props = {
    event: FetchedEvent;
}

const BASE_URL = "https://jkpg.com";

export default function EventCard({ event }: Props) {
    const fullLink = event.link?.startsWith("http") ? event.link : `${BASE_URL}${event.link}`;
    
    // Clean up time text by removing extra whitespace and newlines
    const cleanTime = event.startTime?.replace(/\s+/g, ' ').trim() || 'Tid ej angiven';
    
    // Handle missing or placeholder images
    const hasValidImage = event.imageUrl && !event.imageUrl.includes('placeholder');
    
    return (
        <Card className="cursor-pointer hover:shadow-lg transition-all duration-200 hover:scale-[1.02] h-full flex flex-col p-0 overflow-hidden">
            <CardHeader className="p-0 relative">
                <a href={fullLink} target="_blank" rel="noopener noreferrer" className="block">
                    {hasValidImage ? (
                        <img
                            src={event.imageUrl}
                            alt={event.title}
                            className=" object-cover w-full h-48"
                            onError={(e) => {
                                // Fallback if image fails to load
                                e.currentTarget.style.display = 'none';
                                e.currentTarget.nextElementSibling?.classList.remove('hidden');
                            }}
                        />
                    ) : null}
                    
                    {/* Fallback placeholder */}
                    <div className={`${hasValidImage ? 'hidden' : 'flex'} items-center justify-center h-48 bg-gradient-to-br from-blue-50 to-indigo-100 rounded-t-lg`}>
                        <div className="text-center">
                            <Calendar className="w-12 h-12 text-indigo-400 mx-auto mb-2" />
                            <p className="text-sm text-indigo-600 font-medium">Evenemang</p>
                        </div>
                    </div>
                    
                    {/* External link indicator */}
                    <div className="absolute top-2 right-2 bg-white/90 backdrop-blur-sm rounded-full p-1.5 opacity-0 group-hover:opacity-100 transition-opacity">
                        <ExternalLink className="w-4 h-4 text-gray-600" />
                    </div>
                </a>
            </CardHeader>
            
            <CardContent className="p-4 flex-1 flex flex-col">
                <CardTitle className="text-lg mb-3 line-clamp-2 leading-tight">
                    {event.title || 'Evenemang utan titel'}
                </CardTitle>
                
                <div className="space-y-2 flex-1">
                    <div className="flex items-start gap-2 text-sm text-gray-600">
                        <Calendar className="w-4 h-4 mt-0.5 flex-shrink-0 text-indigo-500" />
                        <span>{event.startDate || 'Datum ej angivet'}</span>
                    </div>
                    
                    <div className="flex items-start gap-2 text-sm text-gray-600">
                        <Clock className="w-4 h-4 mt-0.5 flex-shrink-0 text-indigo-500" />
                        <span className="break-words">{cleanTime}</span>
                    </div>
                    
                    {event.location && (
                        <div className="flex items-start gap-2 text-sm text-gray-600">
                            <MapPin className="w-4 h-4 mt-0.5 flex-shrink-0 text-indigo-500" />
                            <span className="break-words">{event.location}</span>
                        </div>
                    )}
                </div>
                
                {/* Category badge */}
                {event.category && (
                    <div className="mt-3 pt-3 border-t border-gray-100">
                        <span className="inline-block px-2 py-1 text-xs font-medium bg-indigo-50 text-indigo-700 rounded-full capitalize">
                            {event.category}
                        </span>
                    </div>
                )}
            </CardContent>
        </Card>
    )
}