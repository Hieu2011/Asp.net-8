type FormErrorProps = { message: string };

export function FormError({ message }: FormErrorProps) {
  if (!message) return null;
  return (
    <p role="alert" className="rounded-md border border-rose-400/30 bg-rose-950/40 px-3 py-2 text-sm text-rose-200">
      {message}
    </p>
  );
}
