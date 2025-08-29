import { createBrowserRouter, Navigate } from 'react-router'
import App from '../layout/App';
import HomePage from '../../features/home/HomePage';
import TestErrors from '../../features/errors/TestErrors';
import NotFound from '../../features/errors/NotFound';
import ServerError from '../../features/errors/ServerError';
import EventsDashboard from '@/features/events/EventsDashboard';

export const router = createBrowserRouter([
    {
        path: '/',
        element: <App />,
        children: [
            { path: '', element: <HomePage /> },
            {path: 'events', element: <EventsDashboard/>},
            { path: 'errors', element: <TestErrors /> },
            { path: 'not-found', element: <NotFound /> },
            { path: 'server-error', element: <ServerError /> },
            { path: '*', element: <Navigate replace to="/not-found" /> },

        ]
    }
]);