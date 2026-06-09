import { makeAutoObservable } from "mobx";

export class EventStore {
    search = '';
    startDate: Date | undefined = undefined;

    constructor() {
        makeAutoObservable(this);
    }

    setSearch = (search: string) => {
        this.search = search;
    }

    setStartDate = (date: Date | undefined) => {
        this.startDate = date;
    }
}
