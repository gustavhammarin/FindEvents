import { makeAutoObservable } from "mobx";

export class EventStore {
    filter = 'all';
    startDate = new Date().toISOString();
    search = '';

    constructor() {
        makeAutoObservable(this);
    }

    setFilter = (filter: string) => {
        this.filter = filter;
    }

    setStartDate = (date: Date) => {
        this.startDate = date.toISOString();
    }

    setSearch = (search: string) => {
        this.search = search;
    }
}
