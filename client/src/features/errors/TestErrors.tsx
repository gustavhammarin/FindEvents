import { useMutation } from "@tanstack/react-query";
import agent from "../../lib/api/agent.ts";
import { useState } from "react";

export default function TestErrors() {
  const [errors, setErrors] = useState<string[]>([]);

  const { mutate } = useMutation({
    mutationFn: async ({ path, method = "get" }: { path: string; method: string }) => {
      if (method === "post") await agent.post(path, {});
      else await agent.get(path);
    },
    onError: (err) => {
      if (Array.isArray(err)) setErrors(err);
      else setErrors([]);
    },
  });

  const buttons = [
    { label: "Not found", path: "buggy/not-found", method: "get" },
    { label: "Bad request", path: "buggy/bad-request", method: "get" },
    { label: "Server error", path: "buggy/server-error", method: "get" },
    { label: "Unauthorised", path: "buggy/unauthorised", method: "get" },
  ];

  return (
    <div className="space-y-4 max-w-lg">
      <h2 className="text-lg font-semibold text-gray-900">Test errors</h2>
      <div className="flex flex-wrap gap-2">
        {buttons.map((b) => (
          <button
            key={b.label}
            onClick={() => mutate({ path: b.path, method: b.method })}
            className="px-3 py-1.5 text-sm bg-gray-900 text-white rounded-lg hover:bg-gray-700 transition-colors"
          >
            {b.label}
          </button>
        ))}
      </div>
      {errors.map((err, i) => (
        <p key={i} className="text-sm text-red-600 bg-red-50 border border-red-100 rounded-lg px-3 py-2">
          {err}
        </p>
      ))}
    </div>
  );
}
