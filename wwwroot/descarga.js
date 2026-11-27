window.descargarArchivo = async (nombre, streamRef) => {
    const buf = await streamRef.arrayBuffer();
    const url = URL.createObjectURL(new Blob([buf], { type: "application/zip" }));
    const a = document.createElement("a");
    a.href = url; a.download = nombre;
    document.body.appendChild(a); a.click(); a.remove();
    URL.revokeObjectURL(url);
};
