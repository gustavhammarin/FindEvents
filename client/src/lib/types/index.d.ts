
type EventCursor = {
  startDate: string
  id: string
}

type PagedList<T, TCursor> = {
  items: T[],
  nextCursor: TCursor
}

type FetchedEvent = {
  id: string,
  title: string,
  imageUrl: string,
  startDate: string,
  endDate: string,
  startTime: string,
  endTime: string,
  location: string,
  link: string,
  source: string,
  municipality: string,
  category: string
}

type Activity = {
  id: string;
  title: string;
  date: Date;
  description: string;
  category: string;
  isCancelled: boolean;
  venue: string;
  city: string;
  latitude: number;
  longitude: number;
  attendees: Profile[];
  isGoing: boolean
  isHost: boolean
  hostId: string
  hostDisplayName: string
  hostImageUrl?: string
};

type Profile = {
  id: string
  displayName: string
  bio?: string
  imageUrl?: string
  followersCount?: number
  followingCount?: number
  following?: boolean
}

type Photo = {
  id: string
  url: string
  cols: number
  rows: number
}

type User = {
  id: string,
  email: string,
  displayName: string,
  imageUrl?: string
};

type ChatComment = {
  id: string,
  createdAt: Date,
  body: string,
  userId: string,
  displayName: string,
  imageUrl: string

}

type LocationIQSuggestion = {
  place_id: string
  osm_id: string
  osm_type: string
  licence: string
  lat: string
  lon: string
  boundingbox: string[]
  class: string
  type: string
  display_name: string
  display_place: string
  display_address: string
  address: LocationIQAdress
}

type LocationIQAddress = {
  name: string
  house_number: string
  road: string
  suburb?: string
  town?: string
  village?: string
  city: string
  county?: string
  state?: string
  postcode?: string
  country: string
  country_code: string
}