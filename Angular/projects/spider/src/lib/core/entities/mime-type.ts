export class MimeTypes {
    private constructor(public readonly value: string) {}

    static Pdf = new MimeTypes("application/pdf");
    static Zip = new MimeTypes("application/zip");

    static Jpeg = new MimeTypes("image/jpeg");
    static Png = new MimeTypes("image/png");
    static Svg = new MimeTypes("image/svg");
    static Webp = new MimeTypes("image/webp");

    toString() {
        return this.value;
    }
}