import { useLocation } from "react-router";

export default function ServerError() {
  const { state } = useLocation();

  return (
    <div className="max-w-lg mx-auto mt-16 p-8 bg-white border border-gray-200 rounded-xl">
      <h1 className="text-2xl font-semibold text-gray-900 mb-4">
        {state?.error?.message || "Serverfel"}
      </h1>
      <hr className="border-gray-100 mb-4" />
      <p className="text-sm text-gray-500">
        {state?.error?.details || "Ett internt serverfel inträffade."}
      </p>
    </div>
  );
}
