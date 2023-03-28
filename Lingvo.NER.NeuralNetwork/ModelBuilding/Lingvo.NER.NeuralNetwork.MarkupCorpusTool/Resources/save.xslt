<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet version="1.0"
                xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:msxsl="urn:schemas-microsoft-com:xslt"
                exclude-result-prefixes="msxsl"
                >
  <xsl:output method="text" indent="no" encoding="utf-8" />

  <xsl:template match="/">
    <xsl:apply-templates select="text/sent" />
  </xsl:template>

  <xsl:template match="sent">
    <xsl:apply-templates select="span" />
    <xsl:text>&#x0d;&#x0a;</xsl:text>
  </xsl:template>

  <xsl:template match="span">
    <xsl:value-of select="normalize-space(.)" />
    <xsl:text>&#x20;</xsl:text>
  </xsl:template>

  <xsl:template match="span[ @class ]">
    <xsl:text disable-output-escaping="yes">&lt;</xsl:text>
    <xsl:value-of select="@class"/>
    <xsl:text disable-output-escaping="yes">&gt;</xsl:text>
    <xsl:value-of select="normalize-space(.)" />
    <xsl:text disable-output-escaping="yes">&lt;/</xsl:text>
    <xsl:value-of select="@class"/>
    <xsl:text disable-output-escaping="yes">&gt;</xsl:text>
    <xsl:text>&#x20;</xsl:text>
  </xsl:template>
 
</xsl:stylesheet>
