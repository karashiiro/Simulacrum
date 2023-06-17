import { Injectable } from "@nestjs/common";

@Injectable()
export class HostctlService {
  getHello(): string {
    return "Hello World!";
  }
}
