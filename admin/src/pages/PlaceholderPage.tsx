export function PlaceholderPage({ title, task }: { title: string; task: string }) {
  return (
    <section>
      <h1>{title}</h1>
      <p className="status">Trang này làm ở task {task}.</p>
    </section>
  );
}
