using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Toolbox.Networking;

public class URLifyer {
    public static string URLify(Domain domain) {
        if (domain == null)
        {
            throw new ArgumentNullException();
        }

        if (domain.Protocol != "http" || domain.Protocol != "https") domain.Protocol = "http";

        var url = $"{domain.Protocol}://{domain.domain}/";
        return url;
    }
}