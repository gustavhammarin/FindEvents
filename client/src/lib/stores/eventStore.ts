import { makeAutoObservable } from "mobx";

export class EventStore {
    search = '';
    startDate: Date | undefined = undefined;
    category = '';

    constructor() {
        makeAutoObservable(this);
    }

    setSearch = (search: string) => {
        this.search = search;
    }

    setStartDate = (date: Date | undefined) => {
        this.startDate = date;
    }

    setCategory = (category: string) => {
        this.category = category;
    }
}
