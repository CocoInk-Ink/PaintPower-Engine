package {
    import flash.external.ExternalInterface;

    public class main {
        public function main() {
            var message:String = "Hello, World! :)";
            log(message);
        }

        public function log(text:String):void {
            if (ExtenalInterface.avalible) {
                ExternalInterface.call("console.log", text);
            }
            trace(text);
        }
    }
}