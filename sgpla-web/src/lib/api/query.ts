import createQueryClient from "openapi-react-query";
import { client } from "@/lib/api/client";

export const $api = createQueryClient(client);
