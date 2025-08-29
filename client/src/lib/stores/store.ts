import { createContext } from "react"
import { UiStore } from "./uiStore"
import { EventStore } from "./eventStore"

interface Store {
    uiStore: UiStore
    eventStore: EventStore
}

export const store: Store = {
    uiStore: new UiStore(),
    eventStore: new EventStore(),
}

export const StoreContext = createContext(store)