export default async function handler(req, res) {
    const BACKEND_BASE = process.env.BACKEND_BASE;
  
    if (!BACKEND_BASE) {
      return res.status(500).json({ error: "BACKEND_BASE not set" });
    }
  
    try {
      const proxyPath = req.query.proxy?.join("/") || "";
  
      const queryIndex = req.url.indexOf("?");
      const queryString = queryIndex !== -1 ? req.url.slice(queryIndex) : "";
  
      const targetUrl = `${BACKEND_BASE}/${proxyPath}${queryString}`;
  
      // Forward request
      const response = await fetch(targetUrl, {
        method: req.method,
        headers: {
          ...req.headers,
          host: undefined, 
        },
        body:
          req.method === "GET" || req.method === "HEAD"
            ? undefined
            : JSON.stringify(req.body),
      });
  
      const data = await response.arrayBuffer();
  
      // Forward status + headers
      res.status(response.status);
      response.headers.forEach((v, k) => res.setHeader(k, v));
  
      res.setHeader("Access-Control-Allow-Origin", "*");
      res.setHeader(
        "Access-Control-Allow-Methods",
        "GET,POST,PUT,DELETE,OPTIONS"
      );
      res.setHeader(
        "Access-Control-Allow-Headers",
        "Content-Type, Authorization"
      );
  
      if (req.method === "OPTIONS") {
        return res.status(200).end();
      }
  
      res.send(Buffer.from(data));
    } catch (err) {
      console.error("Proxy error:", err);
      res.status(500).json({ error: "Proxy failed" });
    }
  }
  