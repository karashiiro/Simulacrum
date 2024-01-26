export function unknownAsErr(err: unknown): Error {
  if (err instanceof Error) {
    return err;
  }

  try {
    // Throw the error to populate it with a stack trace
    throw new Error(`${err}`);
  } catch (err) {
    return err as Error;
  }
}
