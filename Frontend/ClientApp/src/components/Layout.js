import React, { Component } from 'react';
import Container from "react-bootstrap/Container";
import Row from "react-bootstrap/Row";
import { NavMenu } from './NavMenu';

export class Layout extends Component {
  displayName = Layout.name

  render() {
    return (
      <Container fluid>
        <Row>
          <NavMenu />
        </Row>
        <Row>
          {this.props.children}
        </Row>
      </Container>
    );
  }
}
