import { Link } from "@tanstack/react-router";
import {
  Breadcrumb,
  BreadcrumbItem,
  BreadcrumbLink,
  BreadcrumbList,
  BreadcrumbPage,
  BreadcrumbSeparator,
} from "@/shared/ui/breadcrumb";

type PanelBreadcrumbProps = {
  activeId: string;
  title: string;
};

export function PanelBreadcrumb({ activeId, title }: PanelBreadcrumbProps) {
  return (
    <Breadcrumb>
      <BreadcrumbList>
        <BreadcrumbItem>
          <BreadcrumbLink render={(props) => <Link to="/" {...props} />}>
            Inicio
          </BreadcrumbLink>
        </BreadcrumbItem>
        {/* La key por ruta hace que el item saliente y el entrante se animen al navegar. */}
        <BreadcrumbItem key={activeId}>
          <BreadcrumbSeparator />
          <BreadcrumbPage>{title}</BreadcrumbPage>
        </BreadcrumbItem>
      </BreadcrumbList>
    </Breadcrumb>
  );
}
