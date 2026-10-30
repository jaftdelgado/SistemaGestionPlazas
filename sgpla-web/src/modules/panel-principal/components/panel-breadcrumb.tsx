import { Link } from "@tanstack/react-router";
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from "@/components/ui/breadcrumb";
import { useNavigation } from "./use-navigation";

export function PanelBreadcrumb() {
  const { items, active } = useNavigation();
  const pageTitle =
    active === "inicio"
      ? "Panel principal"
      : items.find((e) => e.id === active)?.title;
  return (
    <Breadcrumb>
      <BreadcrumbList>
        <BreadcrumbItem>
          <BreadcrumbLink render={(props) => <Link to="/" {...props} />}>
            Inicio
          </BreadcrumbLink>
        </BreadcrumbItem>
        {/* La key por ruta hace que el item saliente y el entrante se animen al navegar. */}
        <BreadcrumbItem key={active}>
          <BreadcrumbSeparator />
          <BreadcrumbPage>{pageTitle}</BreadcrumbPage>
        </BreadcrumbItem>
      </BreadcrumbList>
    </Breadcrumb>
  );
}
