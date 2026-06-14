import axios from "axios";
import { router } from "../../app/router/Routes";

const agent = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
  withCredentials: true,
});

agent.interceptors.response.use(
  (response) => response,
  (error) => {
    const { status, data } = error.response ?? {};
    switch (status) {
      case 404:
        router.navigate("/not-found");
        break;
      case 500:
        router.navigate("/server-error", { state: { error: data } });
        break;
    }
    return Promise.reject(error);
  }
);

export default agent;
