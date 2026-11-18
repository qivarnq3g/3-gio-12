type ProblemDetailsLike = {
  title?: string;
  detail?: string;
  status?: number;
  errors?: Record<string, string[]>;
};

export function getErrorMessage(error: unknown, fallback: string): string {
  if (!error) return fallback;
  if (typeof error === "string") return error;
  if (typeof error === "object") {
    const err = error as ProblemDetailsLike;
    if (err.errors && typeof err.errors === "object") {
      const keys = Object.keys(err.errors);
      const firstKey = keys[0];
      if (firstKey !== undefined) {
        const errorList = err.errors[firstKey];
        if (Array.isArray(errorList) && errorList.length > 0 && typeof errorList[0] === "string") {
          return errorList[0];
        }
      }
    }
    if (err.detail) return err.detail;
    if (err.title && err.title !== "One or more validation errors occurred.") return err.title;
  }
  return fallback;
}
