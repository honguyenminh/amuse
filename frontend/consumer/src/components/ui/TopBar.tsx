import { Text } from "./Text";

type TopBarProps = {
  title: string;
  trailing?: React.ReactNode;
};

export function TopBar({ title, trailing }: TopBarProps) {
  return (
    <header className="flex items-center justify-between border-b-2 border-outline bg-surface px-4 py-3">
      <Text as="h1" variant="title-large">
        {title}
      </Text>
      {trailing}
    </header>
  );
}
